using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quote_cs.Controllers;
using quote_cs.Data;
using quote_cs.Models;

namespace quote_cs.Test.Controllers
{
    public class QuotesControllerTest
    {
        [Fact]
        public void GetAllQuotes_ReturnsOkResult_WithListOfQuotes()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDatabase")
                            .Options;

            // Seed the in-memory database
            using (var context = new ApplicationDbContext(options))
            {
                context.Quotes.AddRange(new List<Quote>
                {
                    new Quote { Id = 1, QuoteText = "Test Quote 1", Author = "Author 1" },
                    new Quote { Id = 2, QuoteText = "Test Quote 2", Author = "Author 2" }
                });
                context.SaveChanges();
            }

            // Use a new context instance to act as a request's DbContext
            using (var context = new ApplicationDbContext(options))
            {
                var controller = new QuotesController(context, null);

                // Act
                var result = controller.GetAllQuotes();

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var quotes = Assert.IsType<List<Quote>>(okResult.Value);
                Assert.Equal(2, quotes.Count); // Validate number of quotes returned
            }
        }
    }
}