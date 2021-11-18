using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using VacationRental.Api.Models;

namespace VacationRental.Api.Controllers
{
    [Route("api/v1/bookings")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IDictionary<int, RentalViewModel> _rentals;
        private readonly IDictionary<int, BookingViewModel> _bookings;

        public BookingsController(
            IDictionary<int, RentalViewModel> rentals,
            IDictionary<int, BookingViewModel> bookings)
        {
            _rentals = rentals;
            _bookings = bookings;
        }

        [HttpGet]
        [Route("{bookingId:int}")]
        public BookingViewModel Get(int bookingId)
        {
            if (!_bookings.ContainsKey(bookingId))
                throw new ApplicationException("Booking not found");

            return _bookings[bookingId];
        }

        [HttpPost]
        public ResourceIdViewModel Post(BookingBindingModel model)
        {
            if (model.Nights <= 0)
                throw new ApplicationException("Nigts must be positive");
            if (!_rentals.ContainsKey(model.RentalId))
                throw new ApplicationException("Rental not found");

            int preparationTimeInDays = _rentals[model.RentalId].PreparationTimeInDays;
            var rentalBookings = _bookings.Where(x => x.Value.RentalId == model.RentalId).ToDictionary(x => x.Key, x => x.Value); // get all bookings for rental with equals rentalId

            var unitsAvailability = new bool[_rentals[model.RentalId].Units]; // Create map for available units. Use this info in calendar
            foreach (var booking in rentalBookings.Values)
            {
                var existingBookingEndDate = booking.Start.AddDays(booking.Nights + preparationTimeInDays);
                var newBookingEndDate = model.Start.AddDays(model.Nights + preparationTimeInDays);

                if ((booking.Start <= model.Start.Date && existingBookingEndDate > model.Start.Date)
                    || (booking.Start < newBookingEndDate && existingBookingEndDate >= newBookingEndDate)
                    || (booking.Start > model.Start && existingBookingEndDate < newBookingEndDate))
                {
                    unitsAvailability[booking.Unit - 1] = true;// mark booked units
                }
            }
            if (unitsAvailability.All(x => x))
                throw new ApplicationException("Not available");

            var availableUnit = Array.IndexOf(unitsAvailability, false) + 1;

            var key = new ResourceIdViewModel { Id = _bookings.Keys.Count + 1, Unit = availableUnit };

            _bookings.Add(key.Id, new BookingViewModel
            {
                Id = key.Id,
                Nights = model.Nights,
                RentalId = model.RentalId,
                Start = model.Start.Date,
                Unit = availableUnit
            });

            return key;
        }
    }
}
