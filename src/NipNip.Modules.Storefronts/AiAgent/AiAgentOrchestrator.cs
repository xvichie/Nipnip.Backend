using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;

namespace NipNip.Modules.Storefronts.AiAgent;

// Runs one turn of the tool-use loop for a conversation: reads the persisted history
// (including whatever inbound message the caller already appended), calls Claude with
// the six store tools available, executes any tool calls, and persists + returns the
// resulting reply. Callers are responsible for actually delivering the reply (Messenger
// send, or just returning it — see MessengerWebhookController and the debug endpoint).
public class AiAgentOrchestrator(
    AnthropicClient anthropic,
    AiAgentToolService tools,
    KnowledgeBaseService knowledgeBase,
    ConversationService conversations,
    AppDbContext db,
    ILogger<AiAgentOrchestrator> logger)
{
    private const int MaxToolIterations = 6;

    public async Task<string> RunTurnAsync(Guid storeId, Guid conversationId)
    {
        var store = await db.Stores.FirstAsync(s => s.Id == storeId);
        var history = await conversations.GetHistoryAsync(conversationId);

        List<MessageParam> messages = history
            .Select(m => new MessageParam
            {
                Role = m.Direction == nameof(ConversationMessageDirection.Outbound) ? Role.Assistant : Role.User,
                Content = m.Content,
            })
            .ToList();

        var replyText = await RunLoopAsync(store, conversationId, messages);

        await conversations.AppendMessageAsync(conversationId, ConversationMessageDirection.Outbound, replyText, null);

        return replyText;
    }

    private async Task<string> RunLoopAsync(Store store, Guid conversationId, List<MessageParam> messages)
    {
        for (var iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            Message response;
            try
            {
                response = await anthropic.Messages.Create(new MessageCreateParams
                {
                    Model = Model.ClaudeOpus4_8,
                    MaxTokens = 1024,
                    System = BuildSystemPrompt(store),
                    Messages = messages,
                    Tools = BuildToolDefinitions(),
                });
            }
            catch (Exception)
            {
                return "Sorry, I'm having trouble responding right now — someone from our team will follow up.";
            }

            if (response.StopReason != "tool_use")
            {
                var text = string.Join("\n", response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text)).Trim();
                return text.Length > 0 ? text : "Thanks for reaching out! Could you tell me a bit more about what you're looking for?";
            }

            List<ContentBlockParam> assistantContent = [];
            List<ContentBlockParam> toolResults = [];

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out TextBlock? text))
                {
                    assistantContent.Add(new TextBlockParam { Text = text.Text });
                }
                else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
                {
                    assistantContent.Add(new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input,
                    });

                    var result = await DispatchToolAsync(store, conversationId, toolUse.Name, toolUse.Input);
                    toolResults.Add(new ToolResultBlockParam
                    {
                        ToolUseID = toolUse.ID,
                        Content = result,
                    });
                }
            }

            messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantContent });
            messages.Add(new MessageParam { Role = Role.User, Content = toolResults });
        }

        logger.LogWarning("AI agent hit the {Max}-iteration tool-loop cap for conversation {ConversationId}", MaxToolIterations, conversationId);
        return "Sorry, that's taking longer than expected — someone from our team will follow up with you.";
    }

    private async Task<string> DispatchToolAsync(Store store, Guid conversationId, string name, IReadOnlyDictionary<string, JsonElement> input)
    {
        try
        {
            object result = name switch
            {
                "list_products" => await tools.ListProductsAsync(store, GetString(input, "query")),
                "lookup_product" => await tools.LookupProductAsync(store, GetString(input, "url_or_slug") ?? ""),
                "check_availability" => await tools.CheckAvailabilityAsync(store, GetString(input, "product_slug") ?? "", GetStringDictionary(input, "options")),
                "get_checkout_info" => tools.GetCheckoutInfo(store),
                "search_knowledge_base" => await SearchKnowledgeBaseAsync(store, GetString(input, "query") ?? ""),
                "draft_order" => await tools.DraftOrderAsync(
                    store,
                    conversationId,
                    GetString(input, "customer_name") ?? "",
                    GetString(input, "phone") ?? "",
                    GetString(input, "address") ?? "",
                    GetString(input, "email"),
                    GetOrderItems(input),
                    GetString(input, "shipping_zone_id"),
                    GetString(input, "payment_method")),
                _ => new { error = $"Unknown tool '{name}'." },
            };

            var json = JsonSerializer.Serialize(result);
            logger.LogInformation("AI agent tool {Tool} for conversation {ConversationId} -> {Result}", name, conversationId, json);
            return json;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI agent tool {Tool} for conversation {ConversationId} threw", name, conversationId);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<object> SearchKnowledgeBaseAsync(Store store, string query)
    {
        var results = await knowledgeBase.SearchAsync(store, query);
        return results.Count == 0
            ? new { found = false, sections = Array.Empty<object>(), note = "No matching sections in the merchant's knowledge base." }
            : new { found = true, sections = results.Select(r => new { title = r.Title, content = r.Content }) };
    }

    private static string? GetString(IReadOnlyDictionary<string, JsonElement> input, string key) =>
        input.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Dictionary<string, string> GetStringDictionary(IReadOnlyDictionary<string, JsonElement> input, string key)
    {
        var result = new Dictionary<string, string>();
        if (input.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in value.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                    result[prop.Name] = prop.Value.GetString()!;
            }
        }
        return result;
    }

    private static List<DraftOrderItemInput> GetOrderItems(IReadOnlyDictionary<string, JsonElement> input)
    {
        var items = new List<DraftOrderItemInput>();
        if (!input.TryGetValue("items", out var itemsValue) || itemsValue.ValueKind != JsonValueKind.Array)
            return items;

        foreach (var itemEl in itemsValue.EnumerateArray())
        {
            var slug = itemEl.TryGetProperty("product_slug", out var slugEl) && slugEl.ValueKind == JsonValueKind.String ? slugEl.GetString()! : "";
            var quantity = itemEl.TryGetProperty("quantity", out var qtyEl) && qtyEl.ValueKind == JsonValueKind.Number ? qtyEl.GetInt32() : 1;

            var options = new Dictionary<string, string>();
            if (itemEl.TryGetProperty("options", out var optionsEl) && optionsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in optionsEl.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                        options[prop.Name] = prop.Value.GetString()!;
                }
            }

            items.Add(new DraftOrderItemInput(slug, options, quantity));
        }

        return items;
    }

    private static string BuildSystemPrompt(Store store)
    {
        var instructions = string.IsNullOrWhiteSpace(store.AiAgentInstructions)
            ? ""
            : $"\n\nAdditional merchant instructions: {store.AiAgentInstructions}";

        return $"""
            You are the AI shopping assistant for "{store.Name}", an online store, chatting with a customer over Facebook Messenger.

            Use the available tools to answer accurately — never guess at product availability, pricing, or policies:
            - list_products: browse or search the catalog. Use this when the customer asks what you sell, wants recommendations, or names something generically (e.g. "any sneakers?") without a specific link — never say you don't know what's in stock without checking this first. Follow up with lookup_product on a specific result for full details.
            - lookup_product: look up a product by the storefront link or slug the customer shared. Its description often answers sizing/fit questions (e.g. whether to size up).
            - check_availability: confirm a specific size/color/variant is actually in stock before promising it.
            - search_knowledge_base: use this for any policy or informational question — shipping, returns, takeout, warranty, anything the merchant has written up. Don't guess or answer from memory; search first.
            - get_checkout_info: structured checkout data — delivery zones/fees, and which payment methods the merchant actually accepts (cash on delivery / bank transfer) plus the real details for each, e.g. the bank account/IBAN a customer transfers to. Always use this rather than guessing when asked how to pay, or where to send a bank transfer — never invent bank details.
            - draft_order: once the customer has confirmed exactly what they want and given you their name, phone, and delivery address, draft the order. If get_checkout_info listed delivery zones, call it first (or reuse what it already told you) and pass the matching zone's id as shipping_zone_id — draft_order fails without it when the store has zones configured. Only offer a payment method get_checkout_info shows as enabled. Tell the customer it still needs the merchant's confirmation — it is not final yet.

            If a tool call comes back with an error, read it — it's usually something you can fix and retry (e.g. a missing delivery zone or option), not a real failure. Ask the customer for whatever's missing rather than saying something vague went wrong.

            Keep replies short and conversational, like a real person texting back — not a wall of text. Reply in whatever language the customer writes in (usually Georgian or English).{instructions}
            """;
    }

    private static List<ToolUnion> BuildToolDefinitions() =>
    [
        new Tool
        {
            Name = "list_products",
            Description = "Browse or search this store's product catalog. Use this whenever the customer asks what's available, wants a recommendation, or names something generically without a specific product link — always check here before saying you don't have something. Returns each match's name, storefront link, and price.",
            InputSchema = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["query"] = JsonSerializer.SerializeToElement(new { type = "string", description = "Optional search term (e.g. a product name or category word). Omit to list the store's most recent products." }),
                },
            },
        },
        new Tool
        {
            Name = "lookup_product",
            Description = "Look up a product in this store by its storefront link or slug. Returns its name, description, price, options, and any known variant stock overrides.",
            InputSchema = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["url_or_slug"] = JsonSerializer.SerializeToElement(new { type = "string", description = "The product's storefront URL or slug, as the customer shared it." }),
                },
                Required = ["url_or_slug"],
            },
        },
        new Tool
        {
            Name = "check_availability",
            Description = "Check whether a specific option combination (e.g. size, color) of a product is in stock before promising it to the customer.",
            InputSchema = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["product_slug"] = JsonSerializer.SerializeToElement(new { type = "string", description = "The product's slug, from lookup_product." }),
                    ["options"] = JsonSerializer.SerializeToElement(new
                    {
                        type = "object",
                        description = "Map of option name to chosen value, e.g. {\"Size\": \"43\", \"Color\": \"Black\"}. Omit for products with no options.",
                        additionalProperties = new { type = "string" },
                    }),
                },
                Required = ["product_slug"],
            },
        },
        new Tool
        {
            Name = "get_checkout_info",
            Description = "Get this store's delivery zones (id, name, fee), free-shipping threshold, and which payment methods are enabled with their real details (e.g. bank transfer account info). Use this for \"how do I pay\" / \"what's your bank account\" questions and for the zone id draft_order needs — for written shipping/return policy text, use search_knowledge_base instead.",
            InputSchema = new() { Properties = new Dictionary<string, JsonElement>() },
        },
        new Tool
        {
            Name = "search_knowledge_base",
            Description = "Search the merchant's knowledge base for policy or informational content — shipping, returns, takeout, warranty, or anything else the merchant has written up. Always use this before answering a policy question instead of guessing.",
            InputSchema = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["query"] = JsonSerializer.SerializeToElement(new { type = "string", description = "The customer's question, in their own words." }),
                },
                Required = ["query"],
            },
        },
        new Tool
        {
            Name = "draft_order",
            Description = "Draft an order once the customer has confirmed exactly what they want and provided their name, phone, and delivery address. The order goes to the merchant for manual confirmation — it is not final until they approve it.",
            InputSchema = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["customer_name"] = JsonSerializer.SerializeToElement(new { type = "string" }),
                    ["phone"] = JsonSerializer.SerializeToElement(new { type = "string" }),
                    ["address"] = JsonSerializer.SerializeToElement(new { type = "string" }),
                    ["email"] = JsonSerializer.SerializeToElement(new { type = "string", description = "Optional — ask for it, but proceed without it if the customer doesn't have one handy." }),
                    ["items"] = JsonSerializer.SerializeToElement(new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                product_slug = new { type = "string" },
                                options = new { type = "object", additionalProperties = new { type = "string" } },
                                quantity = new { type = "integer" },
                            },
                            required = new[] { "product_slug", "quantity" },
                        },
                    }),
                    ["shipping_zone_id"] = JsonSerializer.SerializeToElement(new { type = "string", description = "The id (not name) of the customer's delivery zone, from get_checkout_info's shippingZones list. Required if that list is non-empty — the order fails without it." }),
                    ["payment_method"] = JsonSerializer.SerializeToElement(new { type = "string", description = "'CashOnDelivery' or 'BankTransfer' — only offer one get_checkout_info shows as enabled. Defaults to CashOnDelivery." }),
                },
                Required = ["customer_name", "phone", "address", "items"],
            },
        },
    ];
}
