using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quote_cs.Data;
using quote_cs.Models;

namespace quote_cs.Controllers
{
    [Route("api/v1/")]
    [ApiController]
    public class QuotesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        // Inject DbContext
        public QuotesController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: api/v1/quotes
        [HttpGet("quotes")]
        public IActionResult GetAllQuotes()
        {
            // Read all quotes from the database
            var quotes = _context.Quotes.ToList();

            // Return them as JSON
            return Ok(quotes);
        }

        // GET: api/v1/quote/1
        [HttpGet("quote/{id}")]
        public IActionResult GetQuoteById(int id)
        {
            // Find the quote in the database
            var quote = _context.Quotes.FirstOrDefault(q => q.Id == id);

            if (quote == null)
            {
                // Return 404 if the quote with the specified ID is not found
                return NotFound(new { message = "Quote not found" });
            }

            // Return the updated quote as a response
            return Ok(quote);
        }

        // GET: api/v1/quote/liked
        [HttpGet("quote/liked")]
        public IActionResult GetAllLikedQuotes()
        {
            // Read all quotes from the database
            var quotes = _context.Quotes
                .Where(q => q.Likes > 0)
                .OrderByDescending(q => q.Likes) //sort descending by Likes
                .ThenBy(q => q.Id) //sort ascending by Id
                .ToList();

            // Return them as JSON
            return Ok(quotes);
        }


        // PATCH: api/v1/quote/1/like
        [HttpPatch("quote/{id}/like")]
        public IActionResult IncrementLike(int id)
        {
            // Find the quote in the database
            var quote = _context.Quotes.FirstOrDefault(q => q.Id == id);

            if (quote == null)
            {
                // Return 0 to indicate no quote is liked
                return Ok(0);
            }

            // Increment the Likes property
            quote.Likes += 1;

            // Save the changes to the database
            _context.SaveChanges();

            // Return the updated quote as a response
            return Ok(quote.Likes);
        }

        // GET: api/v1/quote
        [HttpGet("quote")]
        public async Task<IActionResult> GetRandomQuoteWithEmptyExclusions()
        {
            var emptyExcludeIds = new List<int>();
            return await GetRandomQuote(emptyExcludeIds);
        }

        // POST: api/v1/quote
        [HttpPost("quote")]
        public async Task<IActionResult> GetRandomQuote([FromBody] List<int> excludeIds)
        {
            // 1. Fetch quote excluding specific IDs
            var quote = await _context.Quotes
                .Where(q => !excludeIds.Contains(q.Id)) // Exclude the given IDs
                .OrderBy(q => Guid.NewGuid())           // Random order
                .FirstOrDefaultAsync();                // Fetch one quote

            if (quote != null)
            {
                // If a quote is found, return it
                return Ok(quote);
            }

            // 2. Fetch new list of quotes from Zen API if no quotes are available
            var fetchedQuotes = await FetchQuotesFromZenApi();

            if (fetchedQuotes == null || !fetchedQuotes.Any())
            {
                return NoContent(); // No new quotes received from the external API
            }

            // Insert only new quotes (ensure no duplicates)
            await InsertNewQuotes(fetchedQuotes);

            // Retry the selection process with the new database entries
            quote = await _context.Quotes
                .Where(q => !excludeIds.Contains(q.Id)) // Exclude the given IDs
                .OrderBy(q => Guid.NewGuid())           // Random order
                .FirstOrDefaultAsync();                // Fetch one quote

            // Return the randomly selected quote
            return Ok(quote);
        }

        private async Task<List<Quote>> FetchQuotesFromZenApi()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync("https://zenquotes.io/api/quotes");

            if (!response.IsSuccessStatusCode)
            {
                return new List<Quote>(); // If API call fails, return empty list
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            // Parse JSON response into a list of Quote objects
            var quotes = JsonSerializer.Deserialize<List<dynamic>>(responseContent);

            // Convert dynamic objects into Quote model
            return quotes.Select(q => new Quote
            {
                QuoteText = q.GetProperty("q").GetString(), // Map "q" (quote text) from Zen API response
                Author = q.GetProperty("a").GetString(), // Map "a" (author) from Zen API response
                Likes = 0 // Start with 0 likes for new quotes
            }).ToList();
        }

        private async Task InsertNewQuotes(List<Quote> fetchedQuotes)
        {
            var existingQuotes = await _context.Quotes
                .Select(q => new { q.QuoteText, q.Author })
                .ToListAsync();

            var newQuotes = fetchedQuotes
                .Where(fq => !existingQuotes
                                .Any(eq => eq.QuoteText == fq.QuoteText && eq.Author == fq.Author))
                .ToList();

            if (newQuotes.Any())
            {
                _context.Quotes.AddRange(newQuotes);
                await _context.SaveChangesAsync(); // Persist changes in the database
            }
        }
    }
}
