using Microsoft.EntityFrameworkCore;
using ShopNgocLan.Models;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace ShopNgocLan.Services
{
    public class DummyChatBotService : IChatBotService
    {
        private readonly DBShopNLContext _context;

        public DummyChatBotService(DBShopNLContext context)
        {
            _context = context;
        }

        public async Task<ChatMessage?> HandleCustomerMessageAsync(ChatConversation conversation, ChatMessage customerMessage)
        {
            // Nếu bạn tắt bot cho cuộc hội thoại này thì bỏ qua
            if (!conversation.IsBotActive)
                return null;

            var text = (customerMessage.Content ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Lấy user đại diện Bot
            var botUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Role.TenRole == "Bot");

            if (botUser == null)
                return null;

            string reply;

            // 1. Thử trả lời dựa theo bảng ChatIntent / Pattern / Reply
            var intentReply = await DetectIntentReplyAsync(text);
            if (!string.IsNullOrEmpty(intentReply))
            {
                reply = intentReply;
            }
            else
            {
                // 2. Thử gợi ý sản phẩm nếu không match ý định nào
                var suggest = await BuildProductSuggestionAsync(text);
                if (!string.IsNullOrEmpty(suggest))
                {
                    reply = suggest;
                }
                else
                {
                    // 3. Không hiểu
                    reply =
                        "Ngọc Lan Bot hiện chưa hiểu rõ câu hỏi của bạn 🥲.\n" +
                        "Nếu bạn muốn mình giới thiệu có thể gửi loại sản phẩm bạn muốn.\n" +
                        "Bạn có thể mô tả rõ hơn, hoặc để lại nội dung, nhân viên sẽ hỗ trợ sớm nhất!";
                }
            }

            var botMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderId = botUser.Id,
                SenderType = "Bot",
                Content = reply,
                SentAt = DateTime.Now,
                IsRead = false,
                MetadataJson = null
            };

            _context.ChatMessages.Add(botMessage);
            conversation.LastMessageAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return botMessage;
        }

        // ====================================================================
        // ===============       Ý ĐỊNH (Intent Detection)       ==============
        // ====================================================================

        private async Task<string?> DetectIntentReplyAsync(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return null;

            // chuẩn hóa: lower + trim
            var normalized = originalText.ToLowerInvariant().Trim();
            // bỏ dấu để so khớp không phụ thuộc dấu
            var normalizedNoDiacritics = RemoveDiacritics(normalized);

            // tách từ để xử lý keyword ngắn (hi, ok, vv.)
            var words = Regex.Split(normalizedNoDiacritics, @"\s+")
                             .Where(w => !string.IsNullOrWhiteSpace(w))
                             .ToList();

            var intents = await _context.ChatIntents
                .Where(i => i.TrangThai)
                .Include(i => i.ChatIntentPatterns.Where(p => p.TrangThai))
                .Include(i => i.ChatIntentReplies.Where(r => r.TrangThai))
                .OrderBy(i => i.DoUuTien)
                .ToListAsync();

            foreach (var intent in intents)
            {
                foreach (var pattern in intent.ChatIntentPatterns)
                {
                    bool matched = false;

                    if (pattern.IsRegex)
                    {
                        // Regex nâng cao: chạy trên TEXT KHÔNG DẤU
                        // => nếu dùng Regex, nên viết pattern KHÔNG DẤU
                        matched = Regex.IsMatch(
                            normalizedNoDiacritics,
                            pattern.PatternText ?? string.Empty,
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                        );
                    }
                    else
                    {
                        var rawKw = (pattern.PatternText ?? "").Trim().ToLowerInvariant();
                        if (string.IsNullOrEmpty(rawKw))
                            continue;

                        // bỏ dấu keyword để so với text đã bỏ dấu
                        var kwNoDia = RemoveDiacritics(rawKw);

                        if (kwNoDia.Length <= 3)
                        {
                            // keyword NGẮN (<= 3) → match theo "từ"
                            // tránh 'hi' dính 'ship', 'ok' dính 'look',...
                            matched = words.Contains(kwNoDia);
                        }
                        else
                        {
                            // keyword dài → substring trên bản không dấu
                            matched = normalizedNoDiacritics.Contains(kwNoDia);
                        }
                    }

                    if (matched)
                    {
                        var reply = intent.ChatIntentReplies
                            .Where(r => r.TrangThai)
                            .OrderBy(_ => Guid.NewGuid())
                            .FirstOrDefault();

                        if (reply != null)
                            return reply.ReplyText;
                    }
                }
            }

            return null;
        }

        // Bỏ dấu tiếng Việt (để "shop ơi" ~ "shop oi")
        private string RemoveDiacritics(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ')
                        sb.Append('d');
                    else if (c == 'Đ')
                        sb.Append('D');
                    else
                        sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // ====================================================================
        // ====================   GỢI Ý SẢN PHẨM   =============================
        // ====================================================================

        private async Task<string?> BuildProductSuggestionAsync(string text)
        {
            var keywords = ExtractKeywords(text);
            if (keywords.Count == 0)
                return null;

            var query = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Where(p => p.IsActive == true);

            foreach (var kw in keywords)
            {
                var k = kw.ToLower();
                query = query.Where(p => p.TenSanPham.ToLower().Contains(k));
            }

            var list = await query
                .OrderByDescending(p => p.NgayTao)
                .Take(5)
                .Select(p => new
                {
                    p.Id,
                    p.TenSanPham,
                    TenDanhMuc = p.DanhMuc.TenDanhMuc
                })
                .ToListAsync();

            if (!list.Any())
                return null;

            var lines = new List<string>
    {
        "✨ Mình gợi ý một vài sản phẩm phù hợp với mô tả của bạn:"
    };

            foreach (var p in list)
            {
                lines.Add(
                    $"- {p.TenSanPham} ({p.TenDanhMuc}) – " +
                    $"<a href=\"/Shop/Details/{p.Id}\" target=\"_blank\">Xem sản phẩm</a>"
                );
            }

            lines.Add("Bạn bấm vào link để xem thêm hình, màu và size nhé 🧡");

            return string.Join("\n", lines);
        }


        private List<string> ExtractKeywords(string text)
        {
            var cleaned = Regex.Replace(text, @"[^\p{L}\p{N}\s]", " ");
            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var stopWords = new HashSet<string>
            {
                "cho","tìm","mua","giúp","em","anh","chị","cần","tư", "vấn",
                "một","cái","bộ","size","đồ","bộ đồ"
            };

            var keywords = parts
                .Select(w => w.ToLower())
                .Where(w => w.Length >= 2 && !stopWords.Contains(w))
                .Distinct()
                .Take(6)
                .ToList();

            return keywords;
        }
    }
}
