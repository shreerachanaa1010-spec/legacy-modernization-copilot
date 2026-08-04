using System;
using LegacySampleProject;

Console.WriteLine("Sample project for LegacyModernization analyzer");

var svc = new CustomerService();
svc.TestConfigureAwait().GetAwaiter().GetResult();
Console.WriteLine("Done");
