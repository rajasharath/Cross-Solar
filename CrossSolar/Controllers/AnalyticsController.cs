using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossSolar.Domain;
using CrossSolar.Models;
using CrossSolar.Repository;
using Microsoft.AspNetCore.Mvc;

namespace CrossSolar.Controllers
{
    [Route("panel")]
    public class AnalyticsController : Controller
    {
        private readonly IAnalyticsRepository _analyticsRepository;

        private readonly IPanelRepository _panelRepository;

        public AnalyticsController(IAnalyticsRepository analyticsRepository, IPanelRepository panelRepository)
        {
            _analyticsRepository = analyticsRepository;
            _panelRepository = panelRepository;
        }

        // GET panel/XXXX1111YYYY2222/analytics
        [HttpGet("{panelId}/[controller]")]
        public async Task<IActionResult> Get([FromRoute] string panelId)
        {
            var panel = await _panelRepository.GetAsync(panelId); //Changed the Query() to GetAsync()      

            if (panel == null) return NotFound();

            var analytics = _analyticsRepository.Query().Where(x => x.PanelId.Equals(panelId, StringComparison.CurrentCultureIgnoreCase)).AsEnumerable();

            var result = new OneHourElectricityListModel
            {
                OneHourElectricitys = analytics.Select(c => new OneHourElectricityModel
                {
                    Id = c.Id,
                    KiloWatt = c.KiloWatt,
                    DateTime = c.DateTime
                }).ToList()
            };

            return Ok(result);
        }

        // GET panel/XXXX1111YYYY2222/analytics/day
        [HttpGet("{panelId}/[controller]/day")]
        public async Task<IActionResult> DayResults([FromRoute] string panelId)
        {
            var result = new List<OneDayElectricityModel>();
            var analytics = _analyticsRepository.Query().Where(x => x.PanelId.Equals(panelId, StringComparison.CurrentCultureIgnoreCase)).ToList(); //Here GetAsync() is not used as the panelId is not primary key of OneHourElectricity table

            var distinctDates = analytics.Select(o => o.DateTime).Distinct();

            foreach (var date in distinctDates)
            {
                var dateRecords = analytics.Where(x => x.DateTime == date);

                var dayResult = new OneDayElectricityModel();
                dayResult.Minimum = dateRecords.Min(x => x.KiloWatt);
                dayResult.Maximum = dateRecords.Max(x => x.KiloWatt);
                dayResult.Sum = dateRecords.Sum(x => x.KiloWatt);
                dayResult.Average = dateRecords.Average(x => x.KiloWatt);

                result.Add(dayResult);
            }

            return Ok(result);
        }

        // POST panel/XXXX1111YYYY2222/analytics
        [HttpPost("{panelId}/[controller]")]
        public async Task<IActionResult> Post([FromRoute] string panelId, [FromBody] OneHourElectricityModel value)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var oneHourElectricityContent = new OneHourElectricity
            {
                PanelId = panelId,
                KiloWatt = value.KiloWatt,
                DateTime = DateTime.UtcNow
            };

            await _analyticsRepository.InsertAsync(oneHourElectricityContent);

            var result = new OneHourElectricityModel
            {
                Id = oneHourElectricityContent.Id,
                KiloWatt = oneHourElectricityContent.KiloWatt,
                DateTime = oneHourElectricityContent.DateTime
            };

            return Created($"panel/{panelId}/analytics/{result.Id}", result);
        }
    }
}