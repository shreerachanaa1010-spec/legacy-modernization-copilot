using System;
using Legacy.Services;

Console.WriteLine("Sample project for LegacyModernization analyzer");

var svc = new CustomerService();
Console.WriteLine(svc.GetCustomer());
