using AutoMapper;
using FreeCourse.Services.Catalog.Dtos;
using FreeCourse.Services.Catalog.Models;
using FreeCourse.Services.Catalog.Settings;
using FreeCourse.Shared.Dtos;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeCourse.Services.Catalog.Services
{
    public class CourseService: ICourseService
    {
        private readonly IMongoCollection<Course> _courseCollection;
        private readonly IMongoCollection<Category> _categoryCollection;
        private readonly DatabaseSettings _databaseSettings;

        private IMapper _mapper;

        public CourseService(IMapper mapper,IOptions<DatabaseSettings>  databaseSettings)
        {
            _databaseSettings = databaseSettings.Value;
            var client = new MongoClient(_databaseSettings.ConnectionString);
            var database = client.GetDatabase(_databaseSettings.DatabaseName);
            _courseCollection = database.GetCollection<Course>(_databaseSettings.CourseCollectionName);
            _categoryCollection = database.GetCollection<Category>(_databaseSettings.CategoryCollectionName);
            _mapper = mapper;


        }

        public async Task<Response<List<CourseDto>>> GetAllAsync()
        {
            var courses = await _courseCollection.Find(course => true).ToListAsync();
            courses = courses ?? new List<Course>();

            foreach(var item in courses)
            {
                item.Category = (Category)await _categoryCollection.Find(x => x.Id == item.CategoryId).FirstAsync();
            }

            return Response<List<CourseDto>>.Success(_mapper.Map<List<CourseDto>>(courses), 200);

        }

        public async Task<Response<CourseDto>> GetByIdAsync(string id)
        {
            var course = await _courseCollection.Find((x) => x.Id == id).FirstOrDefaultAsync();
            if (course == null) return Response<CourseDto>.Fail("Course not found", 404);
            course.Category = (Category)await _categoryCollection.Find(x => x.Id == course.CategoryId).FirstAsync();
            return Response<CourseDto>.Success(_mapper.Map<CourseDto>(course), 200);
        }

        public async Task<Response<CourseDto>> GetByAllUserIdAsync(string userId)
        {
            var course = await _courseCollection.Find((x) => x.UserId == userId).FirstOrDefaultAsync();
            if (course == null) return Response<CourseDto>.Fail("Course not found", 404);

            course.Category = (Category)await _categoryCollection.Find(x => x.Id == course.CategoryId).FirstAsync();
            return Response<CourseDto>.Success(_mapper.Map<CourseDto>(course), 200);
        }

        public async Task<Response<CourseDto>> CreateAsync(CourseCreateDto courseDto)
        {
            var course = _mapper.Map<Course>(courseDto);
            course.CreatedTime = DateTime.Now;
            await _courseCollection.InsertOneAsync(course);
            return Response<CourseDto>.Success(_mapper.Map<CourseDto>(course), 200);
        }

        public async Task<Response<NoContent>> UpdateAsync(CourseUpdateDto courseUpdateDto)
        {
            var course = _mapper.Map<Course>(courseUpdateDto);
            var result = await _courseCollection.FindOneAndReplaceAsync((x) => x.Id == courseUpdateDto.Id, course);
            if (result == null) return Response<NoContent>.Fail("Course not found", 404);

            return Response<NoContent>.Success(204);

        }

        public async Task<Response<NoContent>> DeleteAsync(string id)
        {
           var result =  await _courseCollection.DeleteOneAsync(id);
           return (result.DeletedCount > 0 ? Response<NoContent>.Success(204) : Response<NoContent>.Fail("Course not found", 404));
        }

    }
}
