using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroRabbit.Banking.Domain.Models
{
    public class Account
    {
        public int Id { get; set; }

        public string? AccountType { get; set; }

        public decimal AccountBallance { get; set; }
    }
}