using System.Diagnostics.CodeAnalysis;
using Aiursoft.EventsRecorder.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.MySql;

[ExcludeFromCodeCoverage]

public class MySqlContext(DbContextOptions<MySqlContext> options) : EventsRecorderDbContext(options);
