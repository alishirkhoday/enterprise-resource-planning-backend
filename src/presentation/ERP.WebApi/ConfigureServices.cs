using ERP.Application.Common;
using ERP.Application.Common.Interfaces.Authentication;
using ERP.Application.Common.Interfaces.Branches;
using ERP.Application.Common.Interfaces.Identity;
using ERP.Application.Common.Logging;
using ERP.Domain.Repositories.Security;
using ERP.Domain.Repositories.Users;
using ERP.WebApi.DTOs;
using ERP.WebApi.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ERP.WebApi
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITokenService, TokenService>();

            // One object answers both questions, because both come from the same token: who is
            // acting, for the audit columns, and which branches they may read, for the query
            // filter. Registered here rather than in Infrastructure because only the web layer
            // has a request to read them from.
            services.AddHttpContextAccessor();
            services.AddScoped<HttpCurrentUser>();
            services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpCurrentUser>());
            services.AddScoped<IBranchContext>(sp => sp.GetRequiredService<HttpCurrentUser>());
            services.AddCors(options =>
            {
                options.AddPolicy("Default",
                policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
            return services;
        }

        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"] ?? throw new ArgumentNullException(null, "JWT Key is null."))),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents()
                {
                    OnAuthenticationFailed = context =>
                    {
                        //When token expired or invalid
                        return Task.CompletedTask;
                    },
                    // Awaited rather than blocked on: this runs for every authenticated request, and
                    // blocking here would tie up a thread pool thread per request.
                    OnTokenValidated = async context =>
                    {
                        var cancellationToken = context.HttpContext.RequestAborted;

                        var token = context.Request.Headers.Authorization.ToString();
                        token = token.Replace("Bearer ", "");
                        if (string.IsNullOrWhiteSpace(token))
                        {
                            context.Fail("Token is not existence...!");
                            return;
                        }
                        var handler = new JwtSecurityTokenHandler();
                        var claims = handler.ReadJwtToken(token).Claims;
                        if (claims is null || !claims.Any())
                        {
                            context.Fail("Claim not found");
                            return;
                        }
                        var _userRepository = context.HttpContext.RequestServices.GetService<IUserRepository>();
                        if (_userRepository is null)
                        {
                            context.Fail("UserRepository has been deleted");
                            return;
                        }
                        var userId = claims.FirstOrDefault(x => x.Type == "uid");
                        if (userId is null)
                        {
                            context.Fail("UId has been deleted");
                            return;
                        }
                        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
                        if (user is null)
                        {
                            context.Fail("User not found");
                            return;
                        }
                        var _userTokenRepository = context.HttpContext.RequestServices.GetService<IUserTokenRepository>();
                        if (_userTokenRepository is null)
                        {
                            context.Fail("UserTokenRepository has been deleted");
                            return;
                        }
                        var userToken = await _userTokenRepository.GetByUserAndTokenAsync(user, token.HashSha256(), cancellationToken);
                        if (userToken is null)
                        {
                            context.Fail("User's token has been deleted");
                            return;
                        }
                        if (userToken.ExpireDateTime <= DateTimeOffset.UtcNow)
                        {
                            context.Fail("Token date is expired...!");
                            return;
                        }
                        var securityStamp = claims.FirstOrDefault(x => x.Type == "ss");
                        if (securityStamp is null)
                        {
                            context.Fail("User's SS has been deleted");
                            return;
                        }
                        if (user.SecurityStamp != securityStamp.Value)
                        {
                            context.Fail("User invalid. Please signin again.");
                            return;
                        }
                        var _userPermissionRepository = context.HttpContext.RequestServices.GetService<IPermissionRepository>();
                        if (_userPermissionRepository is null)
                        {
                            context.Fail("UserPermissionRepository has been deleted");
                            return;
                        }
                        var permissions = await _userPermissionRepository.GetPermissionsByUserAsync(
                            user,
                            up => new UserPermissionResultDto(up.Title, up.Value),
                            cancellationToken);
                        if (permissions is null)
                        {
                            context.Fail("User permissions has been deleted");
                            return;
                        }
                        if (context.Principal?.Identity is ClaimsIdentity claimIdentity)
                        {
                            permissions.ForEach(c => claimIdentity.AddClaim(new Claim(c.title, c.value)));
                        }
                    },
                    OnChallenge = context =>
                    {
                        //different errors maybe happen
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        //
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        //when token received this event run and can change token struct
                        return Task.CompletedTask;
                    }
                };
            });
            return services;
        }

        public static IServiceCollection AddAuthorizationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthorization(option =>
            {
                var policies = new Dictionary<string, string>
                {
                    {"CreateNewAccount","CreateNewAccount"}
                };
                foreach (var policy in policies)
                {
                    option.AddPolicy(policy.Value, p => p.RequireClaim(policy.Value));
                }
            });
            return services;
        }

        public static IServiceCollection AddLogServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.With<CreatedAtEnricher>()
                .Enrich.With<SensitiveDataEnricher>()
                .Enrich.FromLogContext();
            //var mongoConnectionString = configuration["ConnectionStrings:LogDatabase"];
            var mongoSettingsHost = configuration["Serilog:WriteTo:0:Args:Settings:Host"];
            var mongoSettingsPort = configuration["Serilog:WriteTo:0:Args:Settings:Port"];
            var mongoDatabaseName = configuration["Serilog:WriteTo:0:Args:DatabaseName"];
            var mongoCollectionName = configuration["Serilog:WriteTo:0:Args:CollectionName"];
            var mongoClientSettings = new MongoClientSettings
            {
                Server = new MongoServerAddress(mongoSettingsHost, int.Parse(mongoSettingsPort ?? throw new ArgumentNullException(mongoSettingsPort))),
                //Credential = MongoCredential.CreateCredential(database, user, pass),
                ConnectTimeout = TimeSpan.FromSeconds(10),
                SocketTimeout = TimeSpan.FromSeconds(30)
            };
            if (environment.IsDevelopment())
            {
                mongoClientSettings.AllowInsecureTls = true;
                mongoClientSettings.UseTls = false;
                mongoClientSettings.IPv6 = false;
            }
            if (environment.IsStaging())
            {
                mongoClientSettings.AllowInsecureTls = false;
                mongoClientSettings.UseTls = true;
                mongoClientSettings.IPv6 = false;
            }
            if (environment.IsProduction())
            {
                mongoClientSettings.AllowInsecureTls = false;
                mongoClientSettings.UseTls = true;
                mongoClientSettings.IPv6 = false;
            }
            var mongoClient = new MongoClient(mongoClientSettings);
            var database = mongoClient.GetDatabase(mongoDatabaseName ?? throw new ArgumentNullException(mongoDatabaseName));
            var collection = database.GetCollection<BsonDocument>(mongoCollectionName ?? throw new ArgumentNullException(mongoCollectionName));
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("CreatedAt");
            var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30) };
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexKeys, indexOptions));
            loggerConfiguration
                .WriteTo.MongoDB(database, default, mongoCollectionName, 50, TimeSpan.FromSeconds(1));

            var filePath = configuration["Serilog:WriteTo:1:Args:Path"];
            var fileRollingInterval = configuration["Serilog:WriteTo:1:Args:RollingInterval"];
            var fileRetainedFileCountLimit = configuration["Serilog:WriteTo:1:Args:RetainedFileCountLimit"];
            loggerConfiguration
                .WriteTo.File(
                    filePath ?? throw new ArgumentNullException(filePath),
                    rollingInterval: Enum.Parse<RollingInterval>(fileRollingInterval ?? throw new ArgumentNullException(fileRollingInterval)),
                    retainedFileCountLimit: int.Parse(fileRetainedFileCountLimit ?? throw new ArgumentNullException(fileRetainedFileCountLimit)));

            if (environment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console();
            }

            var logger = loggerConfiguration.CreateLogger();
            Log.Logger = logger;
            services.AddSingleton<Serilog.ILogger>(logger);
            return services;
        }

        public static IServiceCollection AddSendMessageServices(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection AddApiVersionAndSwaggerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                var openApiInfo = new OpenApiInfo
                {
                    Title = "ERP APIs",
                    Version = "v1.0",
                    Description = "ERP APIs"
                };
                options.SwaggerDoc("v1", openApiInfo);
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "JWT Auth",
                    Description = "Token",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    //Reference = new OpenApiReference
                    //{
                    //    Id = JwtBearerDefaults.AuthenticationScheme,
                    //    Type = ReferenceType.SecurityScheme
                    //}
                };
                var securitySchemeReference = new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme)
                {
                    Reference = new OpenApiReferenceWithDescription
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };
                options.AddSecurityDefinition(securitySchemeReference.Reference.Id, securityScheme);
                //options.AddSecurityRequirement(new OpenApiSecurityRequirement
                //{
                //    {
                //        new OpenApiSecurityScheme
                //        {
                //            Reference = securityScheme.Reference,
                //            Scheme = securityScheme.Scheme,
                //            Name = securityScheme.Name,
                //            In = securityScheme.In,
                //        },
                //        new string[] {}
                //    }
                //});
                options.TagActionsBy(api =>
                {
                    if (api.GroupName is not null)
                    {
                        return [api.GroupName];
                    }
                    return ["Other"];
                });
                options.DocInclusionPredicate((name, api) => true);
                options.UseInlineDefinitionsForEnums();
            });
            return services;
        }
    }
}
