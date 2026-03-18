using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Models;
using BusTicketingSystem.Repositories;

namespace BusTicketingSystem.Services
{
    public class DestinationService
    {
        private readonly IDestinationRepository _destinationRepository;

        public DestinationService(IDestinationRepository destinationRepository)
        {
            _destinationRepository = destinationRepository;
        }

        public async Task<DestinationResponseDto> CreateDestinationAsync(CreateDestinationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DestinationName))
                throw new BadRequestException("Destination name is required.");

            var destination = new Destination
            {
                DestinationName = request.DestinationName.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _destinationRepository.CreateAsync(destination);
            return MapToResponse(destination);
        }

        public async Task<List<DestinationResponseDto>> GetAllDestinationsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var destinations = await _destinationRepository.GetAllAsync(pageNumber, pageSize);
            return destinations.Select(MapToResponse).ToList();
        }

        public async Task<DestinationResponseDto> GetDestinationByIdAsync(int id)
        {
            var destination = await _destinationRepository.GetByIdAsync(id);
            if (destination == null)
                throw new NotFoundException("Destination not found.");

            return MapToResponse(destination);
        }

        public async Task UpdateDestinationAsync(int id, UpdateDestinationRequest request)
        {
            var destination = await _destinationRepository.GetByIdAsync(id);
            if (destination == null)
                throw new NotFoundException("Destination not found.");

            if (string.IsNullOrWhiteSpace(request.DestinationName))
                throw new BadRequestException("Destination name is required.");

            destination.DestinationName = request.DestinationName.Trim();
            destination.Description = request.Description?.Trim() ?? string.Empty;
            destination.IsActive = request.IsActive;
            destination.UpdatedAt = DateTime.UtcNow;

            await _destinationRepository.UpdateAsync(destination);
        }

        public async Task DeleteDestinationAsync(int id)
        {
            var destination = await _destinationRepository.GetByIdAsync(id);
            if (destination == null)
                throw new NotFoundException("Destination not found.");

            await _destinationRepository.DeleteAsync(id);
        }

        private static DestinationResponseDto MapToResponse(Destination destination)
        {
            return new DestinationResponseDto
            {
                DestinationId = destination.DestinationId,
                DestinationName = destination.DestinationName,
                Description = destination.Description,
                IsActive = destination.IsActive
            };
        }
    }
}
