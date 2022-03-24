using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Helperland.Models
{
    public class ServiceRequestDetails
    {
        public ServiceRequest Service { get; set; }
        public ServiceRequestAddress ServiceAddress { get; set; }
    }
}
