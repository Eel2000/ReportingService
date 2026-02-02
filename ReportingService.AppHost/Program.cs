var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var reportingDb = postgres.AddDatabase("reportingdb");

var api = builder.AddProject<Projects.ReportingService_Api>("api")
    .WithReference(reportingDb)
    .WaitFor(reportingDb);

builder.AddProject<Projects.ReportingService_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
