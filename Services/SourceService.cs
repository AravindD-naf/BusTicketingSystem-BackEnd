using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Models;
using BusTicketingSystem.Repositories;

namespace BusTicketingSystem.Services
{
    public class SourceService
    {
        private readonly ISourceRepository _sourceRepository;

        public SourceService(ISourceRepository sourceRepository)
        {
            _sourceRepository = sourceRepository;
        }

        public async Task<SourceResponseDto> CreateSourceAsync(CreateSourceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SourceName))
                throw new BadRequestException("Source name is required.");

            if (await _sourceRepository.ExistsByNameAsync(request.SourceName.Trim()))
                throw new BadRequestException("Source name already exists.");


            var source = new Source
            {
                SourceName = request.SourceName.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };


            await _sourceRepository.CreateAsync(source);
            return MapToResponse(source);
        }

        public async Task<List<SourceResponseDto>> GetAllSourcesAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var sources = await _sourceRepository.GetAllAsync(pageNumber, pageSize);
            return sources.Select(MapToResponse).ToList();
        }

        public async Task<SourceResponseDto> GetSourceByIdAsync(int id)
        {
            var source = await _sourceRepository.GetByIdAsync(id);
            if (source == null)
                throw new NotFoundException("Source not found.");

            return MapToResponse(source);
        }

        public async Task UpdateSourceAsync(int id, UpdateSourceRequest request)
        {
            var source = await _sourceRepository.GetByIdAsync(id);
            if (source == null)
                throw new NotFoundException("Source not found.");

            if (string.IsNullOrWhiteSpace(request.SourceName))
                throw new BadRequestException("Source name is required.");

            source.SourceName = request.SourceName.Trim();
            source.Description = request.Description?.Trim() ?? string.Empty;
            source.IsActive = request.IsActive;
            source.UpdatedAt = DateTime.UtcNow;

            await _sourceRepository.UpdateAsync(source);
        }

        public async Task DeleteSourceAsync(int id)
        {
            var source = await _sourceRepository.GetByIdAsync(id);
            if (source == null)
                throw new NotFoundException("Source not found.");

            await _sourceRepository.DeleteAsync(id);
        }

        private static SourceResponseDto MapToResponse(Source source)
        {
            return new SourceResponseDto
            {
                SourceId = source.SourceId,
                SourceName = source.SourceName,
                Description = source.Description,
                IsActive = source.IsActive
            };
        }
    }
}
