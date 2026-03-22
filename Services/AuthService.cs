using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;

namespace BusTicketingSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;

        public AuthService(IUserRepository userRepository, JwtHelper jwtHelper)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (request == null)
                throw new ValidationException("Registration request cannot be null.", "VAL_NULL_REQUEST");

            var normalizedEmail = request.Email.Trim().ToLower();

            if (await _userRepository.EmailExistsAsync(normalizedEmail))
                throw new BadRequestException("Email already exists");

            var user = new User
            {
                FullName = request.FullName,
                Email = normalizedEmail,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = 2
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var savedUser = await _userRepository.GetByIdWithRoleAsync(user.UserId);
            return _jwtHelper.GenerateToken(savedUser!);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailWithRoleAsync(request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new BadRequestException("Invalid credentials");

            return _jwtHelper.GenerateToken(user);
        }
    }
}