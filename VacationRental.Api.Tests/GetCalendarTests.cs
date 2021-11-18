using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using VacationRental.Api.Models;
using Xunit;

namespace VacationRental.Api.Tests
{
    [Collection("Integration")]
    public class GetCalendarTests
    {
        private readonly HttpClient _client;

        public GetCalendarTests(IntegrationFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task GivenCompleteRequest_WhenGetCalendar_ThenAGetReturnsTheCalculatedCalendar()
        {
            var postRentalRequest = new RentalBindingModel
            {
                Units = 2,
                PreparationTimeInDays = 2
            };

            ResourceIdViewModel postRentalResult;
            using (var postRentalResponse = await _client.PostAsJsonAsync($"/api/v1/rentals", postRentalRequest))
            {
                Assert.True(postRentalResponse.IsSuccessStatusCode);
                postRentalResult = await postRentalResponse.Content.ReadAsAsync<ResourceIdViewModel>();
            }

            var postBooking1Request = new BookingBindingModel
            {
                 RentalId = postRentalResult.Id,
                 Nights = 2,
                 Start = new DateTime(2021, 11, 15)
            };

            ResourceIdViewModel postBooking1Result;
            using (var postBooking1Response = await _client.PostAsJsonAsync($"/api/v1/bookings", postBooking1Request))
            {
                Assert.True(postBooking1Response.IsSuccessStatusCode);
                postBooking1Result = await postBooking1Response.Content.ReadAsAsync<ResourceIdViewModel>();
            }

            var postBooking2Request = new BookingBindingModel
            {
                RentalId = postRentalResult.Id,
                Nights = 2,
                Start = new DateTime(2021, 11, 16)
            };

            ResourceIdViewModel postBooking2Result;
            using (var postBooking2Response = await _client.PostAsJsonAsync($"/api/v1/bookings", postBooking2Request))
            {
                Assert.True(postBooking2Response.IsSuccessStatusCode);
                postBooking2Result = await postBooking2Response.Content.ReadAsAsync<ResourceIdViewModel>();
            }

            using (var getCalendarResponse = await _client.GetAsync($"/api/v1/calendar?rentalId={postRentalResult.Id}&start=2021-11-14&nights=7"))
            {
                Assert.True(getCalendarResponse.IsSuccessStatusCode);

                var getCalendarResult = await getCalendarResponse.Content.ReadAsAsync<CalendarViewModel>();
                
                Assert.Equal(postRentalResult.Id, getCalendarResult.RentalId);
                Assert.Equal(7, getCalendarResult.Dates.Count);

                Assert.Equal(new DateTime(2021, 11, 14), getCalendarResult.Dates[0].Date);
                Assert.Empty(getCalendarResult.Dates[0].Bookings);
                Assert.Empty(getCalendarResult.Dates[0].PreparationTimes);

                Assert.Equal(new DateTime(2021, 11, 15), getCalendarResult.Dates[1].Date);
                Assert.Single(getCalendarResult.Dates[1].Bookings);
                Assert.Contains(getCalendarResult.Dates[1].Bookings, x => x.Id == postBooking1Result.Id);
                Assert.Empty(getCalendarResult.Dates[1].PreparationTimes);

                Assert.Equal(new DateTime(2021, 11, 16), getCalendarResult.Dates[2].Date);
                Assert.Equal(2, getCalendarResult.Dates[2].Bookings.Count);
                Assert.Contains(getCalendarResult.Dates[2].Bookings, x => x.Id == postBooking1Result.Id);
                Assert.Empty(getCalendarResult.Dates[2].PreparationTimes);

                Assert.Equal(new DateTime(2021, 11, 17), getCalendarResult.Dates[3].Date);
                Assert.Single(getCalendarResult.Dates[3].Bookings);
                Assert.Contains(getCalendarResult.Dates[3].Bookings, x => x.Id == postBooking2Result.Id);
                Assert.Single(getCalendarResult.Dates[3].PreparationTimes);
                Assert.Contains(getCalendarResult.Dates[3].PreparationTimes, x => x.Unit == postBooking1Result.Unit);

                Assert.Equal(new DateTime(2021, 11, 18), getCalendarResult.Dates[4].Date);
                Assert.Empty(getCalendarResult.Dates[4].Bookings);
                Assert.Equal(2, getCalendarResult.Dates[4].PreparationTimes.Count);
                Assert.Contains(getCalendarResult.Dates[4].PreparationTimes, x => x.Unit == postBooking1Result.Unit);
                Assert.Contains(getCalendarResult.Dates[4].PreparationTimes, x => x.Unit == postBooking2Result.Unit);

                Assert.Equal(new DateTime(2021, 11, 19), getCalendarResult.Dates[5].Date);
                Assert.Empty(getCalendarResult.Dates[5].Bookings);
                Assert.Single(getCalendarResult.Dates[5].PreparationTimes);
                Assert.Contains(getCalendarResult.Dates[4].PreparationTimes, x => x.Unit == postBooking2Result.Unit);

                Assert.Equal(new DateTime(2021, 11, 20), getCalendarResult.Dates[6].Date);
                Assert.Empty(getCalendarResult.Dates[6].Bookings);
                Assert.Empty(getCalendarResult.Dates[6].PreparationTimes);
            }
        }
    }
}
