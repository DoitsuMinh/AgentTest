using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NetAgent.Abstractions;
using NetAgent.Abstractions.Models;
using NetAgent.Hosting.Extensions;
using NetAgent.LLM.Extensions;
using NetAgent.LLM.Gemini;
using NetAgent.Memory.SemanticQdrant.Models;
using System.Text;

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
        Console.OutputEncoding = Encoding.UTF8;

        // Retrieve QdrantOptions
        var qdrantOptions = serviceProvider.GetRequiredService<IOptions<QdrantOptions>>().Value;

        // Create agents using OpenAIProvider
        var agentFactory = serviceProvider.GetRequiredService<IAgentFactory>();

        // Khởi tạo các agent
        var agentOne = await agentFactory.CreateAgent(new AgentOptions
        {
            Name = "Bray",
            Role = "Bạn là Bray, rapper Việt kiều nổi tiếng của Việt Nam với phong cách rap mạnh mẽ, gai góc và không ngại đụng độ. Bạn nổi tiếng với những cú diss gắt và không ai muốn gây sự. Nguồn cảm hứng rap của bạn ảnh hưởng từ người da màu ở Mỹ",
            Goals = new[]
            {
                "Mở màn trận beef với những câu diss gắt và táo bạo",
                "Thể hiện sự áp đảo về mặt lyric",
                "Khiến đối thủ không thể đáp trả",
                "Ở lần beef cuối cùng, sẽ lấy drama lớn nhất của đổi thủ làm fact"
            },
            Temperature = 0.9f,
            MaxTokens = 1000,
            SystemMessage = "Bạn đang đứng giữa trận beef rap nảy lửa, đám đông đang gào thét xung quanh. Nhiệm vụ của bạn là tung ra những câu rap chất, gắt và sắc bén. Đừng kiêng nể, hãy chơi tới bến. Mỗi phản hồi không được vượt quá 4 câu.",
            PreferredProviders = new[] { "gemini" }
        });

        var agentTwo = await agentFactory.CreateAgent(new AgentOptions
        {
            Name = "Jack",
            Role = "Bạn là Jack, người Bến tre giọng dịu dàng, ca sĩ nhạc pop nổi tiếng Việt Nam nay bước lên sàn đấu rap. Bạn lần đầu rap nhưng với bản tính không ngán ai, cho thấy mình không phải chỉ biết hát ballad.",
            Goals = new[]
            {
                "Đáp trả bằng lời rap thông minh và đầy cảm xúc",
                "Làm đối thủ bất ngờ bằng chiều sâu nội tâm",
                "Chiếm lấy cảm tình khán giả bằng sự tinh tế"
            },
            Temperature = 0.9f,
            MaxTokens = 1000,
            SystemMessage = "Bạn đang trong một trận beef rap nảy lửa trước đám đông cuồng nhiệt. Phong cách của bạn là sự pha trộn giữa cảm xúc và sắc sảo. Đừng nhẹ tay — hãy chứng minh rằng bạn có thể chơi ở bất kỳ địa hình nào. Mỗi phản hồi không được vượt quá 4 câu.",
            PreferredProviders = new[] { "gemini" }
        });

        // Bắt đầu trận beef rap
        var battleLog = new StringBuilder();
        battleLog.AppendLine("\n ***************************** \n");
        battleLog.AppendLine("\n Trận Beef Rap Việt Nam: Bray vs Jack \n");

        // 1. Bray mở màn
        var opener = await agentOne.ProcessAsync(new AgentRequest
        {
            Goal = "Mở màn trận beef rap bằng một cú diss thật gắt. Phải táo bạo, bất ngờ và khiến đám đông bùng nổ."
        });
        battleLog.AppendLine($"Bray: {opener.Output}\n");

        // 2. Jack đáp trả
        var comeback = await agentTwo.ProcessAsync(new AgentRequest
        {
            Goal = $"Đáp lại cú diss này bằng một đoạn rap sâu cay và đậm chất riêng:\n{opener.Output}"
        });
        battleLog.AppendLine($"Jack: {comeback.Output}\n");

        // 3. Bray kết thúc trận với cú đánh cuối
        var finisher = await agentOne.ProcessAsync(new AgentRequest
        {
            Goal = $"Kết thúc trận beef với một cú diss cuối cùng, dập tắt hoàn toàn Jack:\n{comeback.Output}"
        });
        battleLog.AppendLine($"Bray: {finisher.Output}\n");

        // In toàn bộ trận đấu
        Console.WriteLine(battleLog.ToString());



       

        await host.RunAsync();
    }


    // ORIGIN PROMPT
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
}