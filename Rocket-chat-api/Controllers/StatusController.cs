﻿using System;
using System.Collections.Generic;
using System.Linq;
using DAL;
using Domain;
using DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rocket_chat_api.Helper;

namespace Rocket_chat_api.Controllers
{
    [ApiController]

    public class StatusController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;
        
        private readonly AppDbContext _context;

        private readonly IConfiguration _configuration;


        public StatusController(ILogger<LoginController> logger,AppDbContext context, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Method that sets a user as offline in the database.
        /// </summary>
        /// <param name="tokenDto">Object that contains signed token with Id of the user that went offline</param>
        [HttpPost]
        [Route("/api/disconnect")]
        public IActionResult UserDisconnected(TokenDto tokenDto)
        {
            var token = tokenDto.Token;
            Dictionary<string, string> validatedTokenClaims;
            try
            {
                validatedTokenClaims = TokenValidation.ValidateToken(token,_configuration["TOKEN_SIGNATURE"]);
            }
            catch (ArgumentException)
            {
                return BadRequest(new {text = "Validation failed"});
            }

            var userId = int.Parse(validatedTokenClaims["userId"]);
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            user.IsOnline = false; 
            _context.Update(user); 
            _context.SaveChanges(); 
            return Ok();
         }


        /// <summary>
        /// Method for chats 1 to 1 for finding out the status of user (Is he online or offline)
        /// </summary>
        /// <param name="userId"> Id of the user that Gets the request</param>
        /// <param name="chatId"> Array of chats user currently has</param>
        /// <returns> UserStatusDTO with ChatId and Status of user</returns>
        [HttpGet]
        [Route("api/checkactivity")]
        public IActionResult CheckUsersActivity(int userId, ICollection<int> chatId)
        {
            var statuses = new List<UserStatusDTO>();

            try
            {
                foreach (var entry in chatId)
                {
                    statuses.Add(new UserStatusDTO()
                    {
                        ChatId = entry,
                        UserActivity = _context.ChatUsers.Single(user => user.ChatId == entry && user.UserId !=userId).User.IsOnline
                    });
                }
            
                return Ok(statuses);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
        /// <summary>
        /// Method to verify user's email
        /// </summary>
        /// <param name="key"> Secret code generated based of user email</param>
        /// <returns> Redirects to correct page in front-end</returns>
        [HttpGet]
        [Route("api/verify")]
        public IActionResult VerifyEmail(string key)
        {
            var verifiedUser = _context.Users.SingleOrDefault(u => u.VerificationLink ==  key);

            if (verifiedUser != null)
            {
                verifiedUser.EmailVerified = true;
                _context.Users.Update(verifiedUser);
                _context.SaveChanges();
                return Redirect("https://boroma4.github.io/rocket-chat-front/#/vsuccess");
            }
            return Redirect("https://boroma4.github.io/rocket-chat-front/#/vfailed");
        }


        [HttpPost]
        [Route("api/settingsapply")]
        public IActionResult SaveNotificationSettings(TokenDto tokenDto)
        {
            var token = tokenDto.Token;
            Dictionary<string, string> validatedTokenClaims;
            try
            {
                validatedTokenClaims = TokenValidation.ValidateToken(token,_configuration["TOKEN_SIGNATURE"]);
            }
            catch (ArgumentException)
            {
                return BadRequest(new {text = "Validation failed"});
            }

            var userId = int.Parse(validatedTokenClaims["userId"]);
            var sound = bool.Parse(validatedTokenClaims["sound"]);
            var connection = bool.Parse(validatedTokenClaims["connection"]);
            var newChat = bool.Parse(validatedTokenClaims["newchat"]);
            var newMessage = bool.Parse(validatedTokenClaims["newmessage"]);

            var notificationId = _context.Users.Single(user => user.UserId == userId).NotificationSettingsId;
            var notificationEntity = _context.NotificationSettings.SingleOrDefault(settings => settings.NotificationSettingsId == notificationId);

            if (notificationEntity!=null)
            {
                notificationEntity.Sound = sound;
                notificationEntity.ConnectionChanged = connection;
                notificationEntity.NewChatReceived = newChat;
                notificationEntity.NewMessageReceived = newMessage;
                _context.NotificationSettings.Update(notificationEntity);
                _context.SaveChanges();
            }
            
            return Ok("success");
        }
    }
}