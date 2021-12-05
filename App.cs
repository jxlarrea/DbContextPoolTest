using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbContextPoolTest
{
    public class App
    {
        private readonly SimpleDbContext _context;

        public App(SimpleDbContext context)
        {
            _context = context;
        }

        public async Task Run(int runId, CancellationToken cancellationToken)
        {
            try
            {
                await _context.Animals.AsNoTracking().ToListAsync(cancellationToken);
                await _context.Animals.CountAsync(cancellationToken);
                await _context.Animals.AnyAsync(cancellationToken);
          
            }
            catch (System.OperationCanceledException)
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}][{runId}] Canceled.");
            }
            catch(Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("---------------------------------------------------");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("---------------------------------------------------");
            }

        }
    }
}
