using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossSolar.Controllers;
using CrossSolar.Domain;
using CrossSolar.Models;
using CrossSolar.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CrossSolar.Tests.Controller
{
    public class AnalyticsControllerTests
    {
        public AnalyticsControllerTests()
        {
            _analyticsController = new AnalyticsController(_analyticsRepositoryMock.Object, _panelRepositoryMock.Object);
        }

        private readonly AnalyticsController _analyticsController;

        private readonly Mock<IAnalyticsRepository> _analyticsRepositoryMock = new Mock<IAnalyticsRepository>();

        private readonly PanelController _panelController;

        private readonly Mock<IPanelRepository> _panelRepositoryMock = new Mock<IPanelRepository>();

        [Fact]
        public async Task Get_ShouldReturnNotFound()
        {
            // Arrange
            string panelId = "AAAA1111BBBB2222";

            // Act
            var result = await _analyticsController.Get(panelId);

            // Assert           
            var createdResult = result as NotFoundResult;
            Assert.Equal(404, createdResult.StatusCode);
        }

        [Fact]
        public async Task Get_ShouldReturnOneHourElectricity()
        {
            // Arrange
            var panel = new Panel
            {
                Brand = "Areva",
                Latitude = 12.345678,
                Longitude = 98.7655432,
                Serial = "AAAA1111BBBB2222"
            };

            var OneHourElectricity = new OneHourElectricity
            {
                Id = 123,
                KiloWatt = 12345678,
                DateTime = DateTime.Now,
                PanelId = "AAAA1111BBBB2222"
            };

            AnalyticsController mockController = MockPanelAndAnalyticsImplementation(panel, OneHourElectricity);

            // Act
            var result = await mockController.Get("AAAA1111BBBB2222");

            // Assert
            Assert.NotNull(result);          
        }

        private static AnalyticsController MockPanelAndAnalyticsImplementation(Panel panel, OneHourElectricity OneHourElectricity)
        {
            var oneHourElectricitys = new List<OneHourElectricity>();
            oneHourElectricitys.Add(OneHourElectricity);
            IQueryable<OneHourElectricity> queryableElectricities = oneHourElectricitys.AsQueryable();

            // Force DbSet to return the IQueryable members of our converted list object as its data source
            var mockSet = new Mock<DbSet<OneHourElectricity>>();
            mockSet.As<IQueryable<OneHourElectricity>>().Setup(m => m.Provider).Returns(queryableElectricities.Provider);
            mockSet.As<IQueryable<OneHourElectricity>>().Setup(m => m.Expression).Returns(queryableElectricities.Expression);
            mockSet.As<IQueryable<OneHourElectricity>>().Setup(m => m.ElementType).Returns(queryableElectricities.ElementType);
            mockSet.As<IQueryable<OneHourElectricity>>().Setup(m => m.GetEnumerator()).Returns(queryableElectricities.GetEnumerator());

            var crossSolarDbContext = new CrossSolarDbContext();
            crossSolarDbContext.OneHourElectricitys = mockSet.Object;

            var mockAnalyticsRepository = new Mock<IAnalyticsRepository>();
            var mockPanelRepository = new Mock<IPanelRepository>();
            var mockPanelGenericRepository = new Mock<GenericRepository<Panel>>();
            var mockAnalyticsGenericRepository = new Mock<GenericRepository<OneHourElectricity>>();

            var mockController = new AnalyticsController(mockAnalyticsRepository.Object, mockPanelRepository.Object);
            mockPanelRepository.Setup(x => x.GetAsync("AAAA1111BBBB2222")).Returns(Task.FromResult(panel));
            mockAnalyticsRepository.Setup(x => x.Query()).Returns(queryableElectricities);
            return mockController;
        }

        [Fact]
        public async Task DayResults_ShouldReturnNull()
        {
            // Arrange

            // Act
            var result = await _analyticsController.DayResults("AAAA1111BBBB2222");

            // Assert            
            var okObjectResult = result as OkObjectResult;
            Assert.Equal(200, okObjectResult.StatusCode);
            var model = okObjectResult.Value as List<OneDayElectricityModel>;
            Assert.Empty(model);
        }

        [Fact]
        public async Task DayResults_ShouldReturnOneHourElectricityModelList()
        {
            // Arrange
            var panel = new Panel
            {
                Brand = "Areva",
                Latitude = 12.345678,
                Longitude = 98.7655432,
                Serial = "AAAA1111BBBB2222"
            };
            var OneHourElectricity = new OneHourElectricity
            {
                Id = 123,
                KiloWatt = 12345678,
                DateTime = DateTime.Now,
                PanelId = "AAAA1111BBBB2222"
            };

            AnalyticsController mockController = MockPanelAndAnalyticsImplementation(panel, OneHourElectricity);

            var expectedResult = new List<OneDayElectricityModel>();
            var OneDayElectricity = new OneDayElectricityModel
            {
                Sum = 123,
                Average = 12345678,
                Maximum = 10,
                Minimum = 10,
                DateTime = DateTime.Now,
            };
            expectedResult.Add(OneDayElectricity);


            // Act
            var result = await _analyticsController.DayResults("AAAA1111BBBB2222");

            // Assert            
            var createdResult = result as OkObjectResult;
            Assert.Equal(200, createdResult.StatusCode);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Post_ShouldReturnBadRequest()
        {
            // Arrange
            var electricityModel = new OneHourElectricityModel();
            _analyticsController.ModelState.AddModelError("testField", "test Message"); // force validation error

            // Act
            var result = await _analyticsController.Post("AAAA1111BBBB2222", electricityModel);

            // Assert
            var createdResult = result as BadRequestObjectResult;
            Assert.Equal(400, createdResult.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldInsertOneHourElectricityContent()
        {
            // Arrange
            var electricityModel = new OneHourElectricityModel();
            electricityModel.Id = 123;
            electricityModel.KiloWatt = 12345678;
            electricityModel.DateTime = DateTime.Now;

            // Act
            var result = await _analyticsController.Post("AAAA1111BBBB2222", electricityModel);

            // Assert           
            var createdResult = result as CreatedResult;
            Assert.Equal(201, createdResult.StatusCode);
            Assert.Equal("panel/AAAA1111BBBB2222/analytics/0", createdResult.Location);
        }
    }
}
