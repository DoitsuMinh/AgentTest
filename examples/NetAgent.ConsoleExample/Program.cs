using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using NetAgent.Hosting.Extensions;
using NetAgent.Abstractions.Models;
using Microsoft.Extensions.Configuration;
using NetAgent.Abstractions;
using NetAgent.LLM.Extensions;
using NetAgent.LLM.OpenAI;
using NetAgent.Memory.SemanticQdrant.Models;
using Microsoft.Extensions.Options;
using NetAgent.LLM.Gemini;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Read configuration from appsettings.json
        builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

        // Add NetAgent services with configuration
        builder.Services.AddNetAgent(builder.Configuration);

        // Add LLM Caching
        builder.Services.AddLLMCaching(options =>
        {
            options.DefaultExpiration = TimeSpan.FromHours(24); // Cache responses for 24 hours
            options.MaxCacheSize = 1000; // Store up to 1000 responses
        });

        // Configure rate limiting with proper DI
        builder.Services.AddLLMRateLimiting(options =>
        {
            options.RequestsPerMinute = 60;
            options.TokensPerMinute = 100000;
            options.ConcurrentRequests = 5;
            options.EnableAdaptiveThrottling = true;
            options.RetryAfter = TimeSpan.FromSeconds(1);
        });

        // Configure and register health checks and monitoring
        builder.Services.AddLLMMonitoringSystem(
            configureHealthChecks: options => 
            {
                options.Timeout = TimeSpan.FromSeconds(5);
                options.FailureThreshold = 3;
                options.FailureWindow = TimeSpan.FromMinutes(5);
            },
            configureMetrics: options => 
            {
                options.EnableLatencyTracking = true;
                options.EnableTokenCounting = true;
                options.EnableErrorTracking = true;
                options.EnableResponseTracking = true;
            }
        );

        // Register OpenAIProvider as the single LLM provider
        builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("NetAgent:LLM:Gemini"));
        builder.Services.AddSingleton<GeminiLLMProvider>();

        // Register OpenAIProvider via OpenAIProviderPlugin explicitly
        builder.Services.AddSingleton<GeminiLLMProviderPlugin>();
        builder.Services.AddSingleton(sp => sp.GetRequiredService<GeminiLLMProviderPlugin>().CreateProvider(sp));

        // Configure QdrantOptions
        builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection("NetAgent:Memory:qdrant"));

        var host = builder.Build();
        var serviceProvider = host.Services;

        // Retrieve QdrantOptions
        var qdrantOptions = serviceProvider.GetRequiredService<IOptions<QdrantOptions>>().Value;

        // Create agents using OpenAIProvider
        var agentFactory = serviceProvider.GetRequiredService<IAgentFactory>();

        // Initialize agents
        var agentOne = await agentFactory.CreateAgent(new AgentOptions
        {
            Name = "RoastMasterOne",
            Role = "You're a sharp-witted jokester, always ready with a clever comeback. You specialize in classic and clever 'yo mama' jokes.",
            Goals = ["Deliver hilarious comebacks", "Out-roast your opponent"],
            Temperature = 0.9f,
            MaxTokens = 1000,
            SystemMessage = "You're participating in a friendly roast battle using witty 'yo mama' jokes. Stay funny, supper mean. Reply with uncompromising attitude. Do not reply anything related to IT field",
            PreferredProviders = ["gemini"]
        });

        var agentTwo = await agentFactory.CreateAgent(new AgentOptions
        {
            Name = "RoastMasterTwo",
            Role = "You're a confident and bold roaster. Your comebacks are fierce but playful, sticking to classic mom jokes.",
            Goals = ["Win the roast battle", "Crack the best jokes"],
            Temperature = 0.9f,
            MaxTokens = 1000,
            SystemMessage = "You're in a roast battle. Use clever 'yo mama' jokes to one-up your opponent. Reply with uncompromising attitude. Do not reply anything related to IT field",
            PreferredProviders = ["gemini"]
        });

        // Start the roast battle
        var roastLog = new StringBuilder();
        roastLog.AppendLine("\n ***************************** \n");
        roastLog.AppendLine("\n Roast Battle: Yo Mama Edition \n");

        // 1. Agent One starts the battle
        var opener = await agentOne.ProcessAsync(new AgentRequest
        {
            Goal = "Start the roast battle with your best 'yo mama' joke, ."
        });
        roastLog.AppendLine($"RoastMasterOne: {opener.Output}\n");

        // 2. Agent Two hits back
        var comeback = await agentTwo.ProcessAsync(new AgentRequest
        {
            Goal = $"Respond to this roast with a stronger one:\n{opener}"
        });
        roastLog.AppendLine($"RoastMasterTwo: {comeback.Output}\n");

        // 3. Agent One finishes with a final burn
        var finisher = await agentOne.ProcessAsync(new AgentRequest
        {
            Goal = $"Finish the roast battle with a final epic 'yo mama' fat joke in response to this:\n{comeback}"
        });
        roastLog.AppendLine($"RoastMasterOne: {finisher.Output}\n");

        // Output the full roast session
        Console.WriteLine(roastLog.ToString());


        //var developerAgent = await agentFactory.CreateAgent(new AgentOptions
        //{
        //    Name = "Developer",
        //    Role = "You are a senior developer with expertise in Azure and authentication systems. You focus on technical implementation details and best practices.",
        //    Goals = ["Understand technical requirements", "Identify potential technical challenges", "Suggest implementation approach"],
        //    Temperature = 0.7f,
        //    MaxTokens = 2000,
        //    SystemMessage = "You are an expert developer focusing on Azure solutions and best practices. Provide detailed technical insights and implementation strategies.",
        //    Memory = new MemoryOptions
        //    {
        //        MaxTokens = 4000,
        //        RelevanceThreshold = 0.7f
        //    },
        //    PreferredProviders = ["gemini"], // Specify preferred providers for developer agent
        //});

        //var productOwnerAgent = await agentFactory.CreateAgent(new AgentOptions
        //{
        //    Name = "Product Owner",
        //    Role = "You are a product owner who focuses on business value and user experience. You represent stakeholder interests and ensure requirements are clear.",
        //    Goals = ["Clarify business requirements", "Define acceptance criteria", "Ensure user value"],
        //    Temperature = 0.8f,
        //    MaxTokens = 1500,
        //    SystemMessage = "You are a product owner focused on delivering business value. Consider user needs and stakeholder requirements in your responses.",
        //    Memory = new MemoryOptions
        //    {
        //        MaxTokens = 3000,
        //        RelevanceThreshold = 0.8f
        //    },
        //    PreferredProviders = ["gemini"], // Product Owner prefers Claude
        //});

        //var scrumMasterAgent = await agentFactory.CreateAgent(new AgentOptions
        //{
        //    Name = "Scrum Master",
        //    Role = "You are a scrum master who facilitates the discussion, ensures clarity, and helps identify blockers and dependencies.",
        //    Goals = ["Facilitate discussion", "Ensure clear requirements", "Identify blockers and risks"],
        //    Temperature = 0.6f,
        //    MaxTokens = 1500,
        //    SystemMessage = "You are a scrum master responsible for facilitating effective communication and identifying project risks. Keep discussions focused and productive.",
        //    Memory = new MemoryOptions
        //    {
        //        MaxTokens = 3000,
        //        RelevanceThreshold = 0.75f
        //    },
        //    PreferredProviders = ["gemini"], // Scrum Master can use DeepSeek or OpenAI
        //});

        //// Start the discussion about Azure SSO authentication story
        //var discussion = new StringBuilder();
        //discussion.AppendLine("User Story Discussion: Azure SSO Authentication\n");

        //// PO starts by presenting the story
        //var poResponse = await productOwnerAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = "Present the user story for Azure SSO authentication, including business value and acceptance criteria.",
        //});
        //discussion.AppendLine($"Product Owner: {poResponse}\n");

        //// Developer asks clarifying technical questions
        //var devResponse = await developerAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = $"Based on the PO's story: {poResponse}\nAsk specific technical questions about the Azure SSO implementation requirements."
        //});
        //discussion.AppendLine($"Developer: {devResponse}\n");

        //// PO responds to technical questions
        //var poFollowup = await productOwnerAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = $"Address the developer's questions: {devResponse}"
        //});
        //discussion.AppendLine($"Product Owner: {poFollowup}\n");

        //// Scrum Master facilitates and summarizes
        //var smResponse = await scrumMasterAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = $"Based on the discussion so far:\n{discussion}\nSummarize the key points, identify any gaps or risks, and suggest next steps."
        //});
        //discussion.AppendLine($"Scrum Master: {smResponse}\n");

        //// Final technical assessment from developer
        //var devFinal = await developerAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = $"Based on the full discussion:\n{discussion}\nProvide a final technical assessment and confirm if we have enough information to start implementation."
        //});
        //discussion.AppendLine($"Developer: {devFinal}\n");

        //// Final user story compilation by Product Owner for Jira
        //var finalUserStory = await productOwnerAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = $"Compile the final user story for Jira based on the discussion:\n{discussion}\nEnsure it includes business value, acceptance criteria, and any technical considerations."
        //});
        //discussion.AppendLine("\nFinal User Story for Jira:");
        //discussion.AppendLine(finalUserStory.Output);

        //// Print the full discussion
        //Console.WriteLine(discussion.ToString());


        //// Start the discussion about Password Strength Meter story
        //var discussion = new StringBuilder();
        //discussion.AppendLine("User Story Discussion: Password Strength Meter\n");

        //// 1. PO presents the initial user story
        //var poResponse = await productOwnerAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = "Present a concise user story for adding a password strength meter to the user registration page, including business value and acceptance criteria."
        //});
        //discussion.AppendLine($"Product Owner: {poResponse}\n");

        //// 2. Developer gives a brief technical feasibility assessment
        //var devResponse = await developerAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = $"Based on the user story: {poResponse}\nProvide a quick technical assessment, including feasibility and any tools/libraries you'd recommend."
        //});
        //discussion.AppendLine($"Developer: {devResponse}\n");

        //// 3. PO finalizes user story for Jira based on developer input
        //var finalStory = await productOwnerAgent.ProcessAsync(new AgentRequest()
        //{
        //    Goal = $"Finalize the user story for Jira including any technical notes from the developer's response:\n{devResponse}"
        //});
        //discussion.AppendLine("\nFinal User Story for Jira:");
        //discussion.AppendLine(finalStory.Output);

        //// Output the discussion
        //Console.WriteLine(discussion.ToString());

        await host.RunAsync();
    }
}