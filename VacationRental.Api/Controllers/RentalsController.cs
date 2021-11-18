using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using VacationRental.Api.Models;

namespace VacationRental.Api.Controllers
{
    [Route("api/v1/rentals")]
    [ApiController]
    public class RentalsController : ControllerBase
    {
        private readonly IDictionary<int, RentalViewModel> _rentals;
        private readonly IDictionary<int, BookingViewModel> _bookings;

        public RentalsController(IDictionary<int, RentalViewModel> rentals, 
            IDictionary<int, BookingViewModel> bookings)
        {
            _rentals = rentals;
            _bookings = bookings;
        }

        [HttpGet]
        [Route("{rentalId:int}")]
        public RentalViewModel Get(int rentalId)
        {
            if (!_rentals.ContainsKey(rentalId))
                throw new ApplicationException("Rental not found");

            return _rentals[rentalId];
        }

        [HttpPost]
        public ResourceIdViewModel Post(RentalBindingModel model)
        {
            var key = new ResourceIdViewModel { Id = _rentals.Keys.Count + 1 };

            _rentals.Add(key.Id, new RentalViewModel
            {
                Id = key.Id,
                Units = model.Units,
                PreparationTimeInDays = model.PreparationTimeInDays
            });

            return key;
        }

        [HttpPut]
        [Route("{rentalId:int}")]
        public ResourceIdViewModel Put(int rentalId, RentalBindingModel model)
        {
            if (!_rentals.ContainsKey(rentalId))
                throw new ApplicationException("Rental not found");

            if (model.PreparationTimeInDays < 0 || model.Units <= 0 )
                throw new ApplicationException("Model is invalid");

            var key = new ResourceIdViewModel { Id = rentalId };

            var preparationTime = _rentals[rentalId].PreparationTimeInDays;

            var rentalBookings = _bookings.Where(x => x.Value.RentalId == rentalId).ToDictionary(x => x.Key, x => x.Value);

            if (model.Units < _rentals[rentalId].Units) {
                if(rentalBookings.Any(x => x.Value.Unit > model.Units
                    && x.Value.Start.AddDays(x.Value.Nights + preparationTime) >= DateTime.Now))
                    throw new ApplicationException("Rental not updated. You can not decrease count of units");
            }

            if(model.PreparationTimeInDays > _rentals[rentalId].PreparationTimeInDays)
            {
                int count = 0;
                foreach (var booking in rentalBookings.Values)
                {
                    if (rentalBookings.Any(x => x.Value.Start.AddDays(x.Value.Nights + model.PreparationTimeInDays) >= booking.Start)) {
                        count++;
                    }
                }

                if(count > 0)
                    throw new ApplicationException("Rental not updated. You can not increase PreparationTimeInDays duration");
            }
            _rentals[rentalId].Units = model.Units;
            _rentals[rentalId].PreparationTimeInDays = model.PreparationTimeInDays;

            return key;
        }
    }
}
