using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using Foundation.Api.ApiControllers;

namespace BitChangeSetManager.Api
{
    public class DeliveriesController : DefaultDtoSetController<Delivery, DeliveryDto>
    {
        public DeliveriesController(IBitChangeSetManagerRepository<Delivery> deliveriesRepository)
            : base(deliveriesRepository)
        {

        }
    }
}