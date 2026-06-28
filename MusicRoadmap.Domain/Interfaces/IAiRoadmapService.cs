using System.Threading.Tasks;
using MusicRoadmap.Domain.Entities;

namespace MusicRoadmap.Domain.Interfaces;

public interface IAiRoadmapService
{
    Task<string> GenerateRoadmapAsync(Student student);
}