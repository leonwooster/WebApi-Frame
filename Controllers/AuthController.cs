using System;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;

using WebApi.Services;
using WebApi.Models;
using WebApi.Configuration;

namespace WebApi.Controllers
{    
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private IUserService _userService;
        private readonly JwtBearerTokenSettings _jwtBearerTokenSettings; 
        private readonly UserManager<IdentityUser> _userManager;

        public AuthController(
            IOptions<JwtBearerTokenSettings> jwtTokenOptions, 
            IUserService userService, 
            UserManager<IdentityUser> userManager)
        {
            _userService = userService;
            _jwtBearerTokenSettings = jwtTokenOptions.Value; 
            _userManager = userManager;
        }

        #region JWT
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserDetails userDetails)
        {
            if (!ModelState.IsValid || userDetails == null) { 
                return new BadRequestObjectResult(new { Message = "User Registration Failed" }); 
            }

            var identityUser = new IdentityUser() { UserName = userDetails.UserName, Email = userDetails.Email }; 
            var result = await _userManager.CreateAsync(identityUser, userDetails.Password); 

            if (!result.Succeeded)
            {
                var dictionary = new ModelStateDictionary(); foreach (IdentityError error in result.Errors) { dictionary.AddModelError(error.Code, error.Description); }
                return new BadRequestObjectResult(new { Message = "User Registration Failed", Errors = dictionary });
            }
            return Ok(new { Message = "User Registration Successful" });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginCredentials credentials)
        {
            IdentityUser identityUser;
            if ((identityUser = await ValidateUser(credentials)) == null)
            { 
                return new BadRequestObjectResult(new { Message = "Login failed" }); 
            }
            var token = GenerateToken(identityUser); 
            
            return Ok(new { Token = token, Message = "Success" });
        }

        [HttpPost]
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {           
            // Well, What do you want to do here ?	        
            // Wait for token to get expired OR 	        
            // Maintain token cache and invalidate the tokens after logout method is called	        
            return Ok(new { Token = "", Message = "Logged Out" });	    
        }

        private async Task<IdentityUser> ValidateUser(LoginCredentials credentials)
        {
            var identityUser = await _userManager.FindByNameAsync(credentials.Username); 
            if (identityUser != null) { 
                var result = _userManager.PasswordHasher.VerifyHashedPassword(identityUser, identityUser.PasswordHash, credentials.Password); 
                return result == PasswordVerificationResult.Failed ? null : identityUser; 
            }
            return null;
        }

        private object GenerateToken(IdentityUser identityUser)
        {
            var tokenHandler = new JwtSecurityTokenHandler(); 
            var key = Encoding.ASCII.GetBytes(_jwtBearerTokenSettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] 
                { 
                    new Claim(ClaimTypes.Name, identityUser.UserName.ToString()), new Claim(ClaimTypes.Email, identityUser.Email) 
                }),
                Expires = DateTime.UtcNow.AddSeconds(_jwtBearerTokenSettings.ExpiryTimeInSeconds),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Audience = _jwtBearerTokenSettings.Audience,
                Issuer = _jwtBearerTokenSettings.Issuer
            };
            var token = tokenHandler.CreateToken(tokenDescriptor); 
            
            return tokenHandler.WriteToken(token);
        }
        #endregion

        #region Basic Authentication        
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody]AuthenticateModel model)
        {
            var user = await _userService.Authenticate(model.Username, model.Password);

            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            return Ok(user);
        }
        #endregion
    }
}
