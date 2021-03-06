﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DAL;
using DTO;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Rocket_chat_api.Helper
{
    public static class TokenValidation
    {
        private static JwtSecurityTokenHandler jwtHandler { get; set; }
        private static readonly string Signature = "this is my custom Secret key for authnetication";

        static TokenValidation()
        {
            jwtHandler = new JwtSecurityTokenHandler();
        }

        internal static Dictionary<string,string> ValidateToken(string token)
        {
            var readableToken = jwtHandler.CanReadToken(token);

            if (readableToken != true)
            {
                 throw new ArgumentException("The token doesn't seem to be in a proper JWT format.");
            }
            try
            {
                var claimsPrincipal = jwtHandler.ValidateToken(token, TokenValidation.GetValidationParameters(), out var validatedToken);
                return claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Token validation failed");
            }
        }
        
        internal static async Task<string> CreateJwtAsync(UserDTO userDto )
        {
            var claims = await CreateClaimsIdentitiesAsync(userDto);

            // Create JWToken
            var token = jwtHandler.CreateJwtSecurityToken(
                subject: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials:
                new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.Default.GetBytes(Signature)),
                    SecurityAlgorithms.HmacSha256Signature));

            return jwtHandler.WriteToken(token);
        }
        
        private static Task<ClaimsIdentity> CreateClaimsIdentitiesAsync(UserDTO userDto)
        {
            var claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaim(new Claim("userName",userDto.UserName ));
            claimsIdentity.AddClaim(new Claim("userId", userDto.UserId.ToString()));
            claimsIdentity.AddClaim(new Claim("imageUrl",userDto.ImageUrl ?? "" ));
            claimsIdentity.AddClaim(new Claim("isOnline",userDto.IsOnline.ToString() ));
            claimsIdentity.AddClaim(new Claim("notificationSettings", JsonConvert.SerializeObject(new
            {
                notificationSettingsId = userDto.NotificationSettings.NotificationSettingsId,
                sound = userDto.NotificationSettings.Sound,
                newMessageReceived = userDto.NotificationSettings.NewMessageReceived,
                newChatReceived = userDto.NotificationSettings.NewChatReceived,
                connectionChanged = userDto.NotificationSettings.ConnectionChanged,
            })));

            return Task.FromResult(claimsIdentity);
        }
        private static TokenValidationParameters GetValidationParameters()
        {
            // Validate tokens received from client, only signature matters here
            return new TokenValidationParameters()
            {
                ValidateLifetime = false, // Because there is no expiration in the generated token
                ValidateAudience = false, // Because there is no audiance in the generated token
                ValidateIssuer = false,   // Because there is no issuer in the generated token
                ValidIssuer = "Sample",
                ValidAudience = "Sample",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Signature)) // The same key as the one that generate the token
            };
        }
    }
}