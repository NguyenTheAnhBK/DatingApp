using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers {
    [Route ("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase {

        private readonly IAuthRepository _repo;
        private readonly IConfiguration _configuration;
        public AuthController (IAuthRepository repo, IConfiguration configuration) {
            _repo = repo;
            _configuration = configuration;
        }

        [HttpPost ("register")]
        //Khi truyền từ Body: {"username" : "", "password" : ""}
        //Nếu để attribute [FromBody] UserForRegister user thì server nhận được user.username = ""
        //Nếu không để attribute trên thì user nhận được là user.username = null
        public async Task<IActionResult> Register (UserForRegisterDto user) {
            if (!ModelState.IsValid)
                return BadRequest (ModelState);

            user.UserName = user.UserName.ToLower ();
            if (await _repo.UserExists (user.UserName))
                return BadRequest ("Username already exists");

            var userToCrete = new User {
                UserName = user.UserName
            };

            var createdUser = await _repo.Register (userToCrete, user.Password);
            return StatusCode (201);
        }

        [HttpPost ("login")]
        public async Task<IActionResult> Login (UserForLoginDto user) {
            var userForLogin = await _repo.Login (user.UserName.ToLower (), user.Password);

            if (userForLogin == null)
                return Unauthorized ();

            var claims = new [] {
                new Claim (ClaimTypes.NameIdentifier, userForLogin.Id.ToString ()),
                new Claim (ClaimTypes.Name, userForLogin.UserName)
            }; //Không nên lưu các thông tin nhạy cảm ở đây tránh để lộ thông tin user 
            //Decoded: https://jwt.io/

            var key = new SymmetricSecurityKey (Encoding.UTF8
                .GetBytes (_configuration.GetSection ("AppSettings:Token").Value));

            var credentials = new SigningCredentials (key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescripter = new SecurityTokenDescriptor () {
                Subject = new ClaimsIdentity (claims),
                Expires = DateTime.Now.AddDays (1),
                SigningCredentials = credentials
            };

            var tokenHanler = new JwtSecurityTokenHandler ();
            var token = tokenHanler.CreateToken (tokenDescripter);

            return Ok (new {
                token = tokenHanler.WriteToken (token)
            });
        }
    }
}