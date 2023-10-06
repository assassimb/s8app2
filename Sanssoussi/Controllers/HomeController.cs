using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Sanssoussi.Areas.Identity.Data;
using Sanssoussi.Models;

namespace Sanssoussi.Controllers
{
    public class HomeController : Controller
    {
        private readonly SqliteConnection _dbConnection;

        private readonly ILogger<HomeController> _logger;

        private readonly UserManager<SanssoussiUser> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<SanssoussiUser> userManager, IConfiguration configuration)
        {
            this._logger = logger;
            this._userManager = userManager;
            this._dbConnection = new SqliteConnection(configuration.GetConnectionString("SanssoussiContextConnection"));
        }

        public IActionResult Index()
        {
            this.ViewData["Message"] = "Parce que marcher devrait se faire SansSoussi";
            return this.View();
        }

        [HttpGet]
        public async Task<IActionResult> Comments()
        {
            var comments = new List<string>();

            var user = await this._userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.View(comments);
            }

            _logger.LogInformation("Fetching comments for the user at {Time} by {Username}", DateTime.UtcNow, user.UserName);

            var cmd = new SqliteCommand($"Select Comment from Comments where UserId ='{user.Id}'", this._dbConnection);
            this._dbConnection.Open();
            var rd = await cmd.ExecuteReaderAsync();

            while (rd.Read())
            {
                comments.Add(rd.GetString(0));
            }

            rd.Close();
            this._dbConnection.Close();

            _logger.LogInformation($"Fetched {comments.Count} comments for the user at " + "{Time} by {Username}", DateTime.UtcNow, user.UserName);
            return this.View(comments);
        }

        [HttpPost]
        public async Task<IActionResult> Comments(string comment)
        {
            var user = await this._userManager.GetUserAsync(this.User);
            if (user == null)
            {
                throw new InvalidOperationException("Vous devez vous connecter");
            }

            _logger.LogInformation("Attempting to add a comment to the database  at {Time} by {Username}", DateTime.UtcNow, user.UserName);

            var cmd = new SqliteCommand(
                "insert into Comments (UserId, CommentId, Comment) Values (@UserId, @CommentId, @Comment)", this._dbConnection);
                cmd.Parameters.AddWithValue("@UserId", user.Id);
                cmd.Parameters.AddWithValue("@CommentId", Guid.NewGuid());
                cmd.Parameters.AddWithValue("@Comment", System.Net.WebUtility.HtmlEncode(comment));
            this._dbConnection.Open();
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Comment added successfully at {Time} by {Username}", DateTime.UtcNow, user.UserName);
            return this.Ok("Commentaire ajouté");
        }

        public async Task<IActionResult> Search(string searchData)
        {
            var searchResults = new List<string>();

            var user = await this._userManager.GetUserAsync(this.User);
            if (user == null || string.IsNullOrEmpty(searchData))
            {
                return this.View(searchResults);
            }

            _logger.LogInformation("Attempting to search comments in the database at {Time} by {Username}", DateTime.UtcNow, user.UserName);

            var cmd = new SqliteCommand(
            "Select Comment from Comments where UserId = @UserId and Comment like @SearchData", this._dbConnection);
            cmd.Parameters.AddWithValue("@UserId", user.Id);
            cmd.Parameters.AddWithValue("@SearchData", $"%{searchData}%");

            this._dbConnection.Open();
            var rd = await cmd.ExecuteReaderAsync();
            while (rd.Read())
            {
                searchResults.Add(rd.GetString(0));
            }

            rd.Close();
            this._dbConnection.Close();

            _logger.LogInformation($"Searched comments from {searchData} in the database at " + "{Time} by {Username}", DateTime.UtcNow, user.UserName);
            return this.View(searchResults);
        }

        public IActionResult About()
        {
            return this.View();
        }

        public IActionResult Privacy()
        {
            return this.View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Emails()
        {
            return this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Emails(object form)
        {
            var searchResults = new List<string>();

            var user = await this._userManager.GetUserAsync(this.User);
            var roles = await this._userManager.GetRolesAsync(user);
            if (roles.Contains("admin"))
            {
                var cmd = new SqliteCommand("select Email from AspNetUsers", this._dbConnection);
                this._dbConnection.Open();
                var rd = await cmd.ExecuteReaderAsync();
                while (rd.Read())
                {
                    searchResults.Add(rd.GetString(0));
                }

                rd.Close();

                this._dbConnection.Close();
            }

            return this.Json(searchResults);
        }
    }
}