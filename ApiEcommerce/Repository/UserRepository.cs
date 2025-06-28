using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ApiEcommerce.Repository;

public class UserRepository : IUserRepository
{
    public readonly ApplicationDbContext _db;
    private string? secretKey;

    public UserRepository(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        secretKey = configuration.GetValue<string>("ApiSettings:secretKey");
    }


    public User? GetUser(int id)
    {
        return _db.Users.FirstOrDefault(u => u.Id == id);
    }

    public ICollection<User> GetUsers()
    {
        return _db.Users.OrderBy(u => u.Username).ToList();
    }

    public bool IsUniqueUser(string username)
    {
        return !_db.Users.Any(u => u.Username.ToLower().Trim() == username.ToLower().Trim());
    }

    public async Task<UserLoginResponseDto> Login(UserLoginDto userLoginDto)
    {
        if (string.IsNullOrEmpty(userLoginDto.Username))
        {
            return new UserLoginResponseDto() { Token = "", User = null, Message = "El Username es requerido" };
        }

        var user = await _db.Users.FirstOrDefaultAsync<User>(u => u.Username.ToLower().Trim() == userLoginDto.Username.ToLower().Trim());
        if (user == null)
        {
            return new UserLoginResponseDto() { Token = "", User = null, Message = "Username no encontrado" };
        }

        if (!BCrypt.Net.BCrypt.Verify(userLoginDto.Password, user.Password))
        {
            return new UserLoginResponseDto() { Token = "", User = null, Message = "Credenciales incorrectas" };
        }

        // JWT
        var handlerToken = new JwtSecurityTokenHandler();
        if (string.IsNullOrWhiteSpace(secretKey)) throw new InvalidOperationException("SecretKey no esra configurada");

        var key = Encoding.UTF8.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim("id", user.Id.ToString()),
                new Claim("usernama", user.Username),
                new Claim(ClaimTypes.Role, user.Role),
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handlerToken.CreateToken(tokenDescriptor);
        return new UserLoginResponseDto()
        {
            Token = handlerToken.WriteToken(token),
            User = new UserRegisterDto()
            {
                Username = user.Username,
                Name = user.Name,
                Role = user.Role,
                Password = user.Password
            },
            Message = "Usuarion logueado correctamente"
        };
    }

    public async Task<User> Register(CreateUserDto createUserDto)
    {
        var encriptedPassword = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
        User newUser = new User()
        {
            Username = createUserDto.Username ?? "Not Username",
            Name = createUserDto.Name,
            Role = createUserDto.Role,
            Password = encriptedPassword,

        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();
        return newUser;
    }
}
