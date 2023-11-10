using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using MicroRabbit.Banking.Domain.Models;

namespace MicroRabbit.Banking.Data.Context
{
    public class BankingDBContext : DbContext
    {
        public BankingDBContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
    }
}