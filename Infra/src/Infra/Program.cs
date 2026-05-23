using Amazon.CDK;

namespace Infra
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            // CDK_DEFAULT_* は `cdk bootstrap/deploy --profile --region` 時に自動で入ります
            new InfraStack(app, "InfraStack", new StackProps
            {
                Env = new Environment
                {
                    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
                }
            });

            app.Synth();
        }
    }
}
