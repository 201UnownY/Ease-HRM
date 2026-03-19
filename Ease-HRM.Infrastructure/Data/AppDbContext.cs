using System;
using System.Collections.Generic;
using System.Text;
using Ease_HRM.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Data
{
    public class AppDbContext (DbContextOptions<AppDbContext> options): DbContext (options)
    {
        
    }
}
