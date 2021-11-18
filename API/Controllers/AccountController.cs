using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;

        public AccountController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AppUser>> Register (RegisterDto registerDto) 
        {    
            if(await UserExists(registerDto.Username)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();

            var user = new AppUser
            {
                UserName = registerDto.Username.ToLower(), 
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(user);  // tells system to add user to the _context Object doesn't yet write to the db.  
            await _context.SaveChangesAsync();

            return user;

        }

        [HttpPost("login")]
        public async Task <ActionResult <AppUser>> Login(LoginDto loginDto)
        {
            var user = _context.Users.SingleOrDefault(x => x.UserName == loginDto.Username);

            if(user == null) return Unauthorized("Invalid Username");

            // first hash the password 
            var hmac = new HMACSHA512(user.PasswordSalt);

            // compare the hash to existing stored hash
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for(int i =0; i < computedHash.Length; i++)
            {
                if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }

            return user;
        }

        private async Task <bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
            // note how the lamda x => x.UserName refers to AppUser, you can tell by the UserName camelcase.  
            // this is because the specifically called is _context.Users were Users returns an AppUser so the param is automatically set to that if not declared

        }
    }
}