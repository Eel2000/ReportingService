var builder = DistributedApplication.CreateBuilder(args);

// Use local PostgreSQL instance via connection string instead of Docker container
var reportingDb = builder.AddConnectionString("reportingdb");

var api = builder.AddProject<Projects.ReportingService_Api>("api")
    .WithReference(reportingDb);

builder.AddProject<Projects.ReportingService_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
