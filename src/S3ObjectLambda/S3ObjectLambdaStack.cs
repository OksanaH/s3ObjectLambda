using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using System.Collections.Generic;
using S3ObjectLambdaCfnAccessPoint = Amazon.CDK.AWS.S3ObjectLambda.CfnAccessPoint;
using S3ObjectLambdaCfnAccessPointProps = Amazon.CDK.AWS.S3ObjectLambda.CfnAccessPointProps;
using Amazon.CDK.AWS.IAM;

namespace S3ObjectLambda
{
    public class S3ObjectLambdaStack : Stack
    {
        internal S3ObjectLambdaStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var bucket = new Bucket(this, "xmlBucket", new BucketProps
            {
                BucketName = "oxies-xml-bucket",
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            var function = new Function(this, "XMLTransformBody", new FunctionProps
            {
                Runtime = Runtime.DOTNET_CORE_3_1,
                Code = Code.FromAsset("./TransformXMLLambda/bin/Release/netcoreapp3.1/publish"),
                Handler = "TransformXML.Lambda::TransformXML.Lambda.Handler::Transform",
                FunctionName = "XMLTransform",
                Timeout = Duration.Minutes(1)
            });

            //policy
            var policy = new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "s3-object-lambda:WriteGetObjectResponse" },
                Resources = new[] { "*" }
            });

            function.AddToRolePolicy(policy);

            var accessPoint = new Amazon.CDK.AWS.S3.CfnAccessPoint(this, "S3AccessPoint", new Amazon.CDK.AWS.S3.CfnAccessPointProps
            {
                Bucket = bucket.BucketName,
                Name = $"supporting"
            });

            var supportingAccessPoint = FormatArn(new ArnComponents()
            {
                Service = "s3",
                Resource = $"accesspoint/{accessPoint.Name}"
            });
            
            var objectLambdaAccessPoint = new S3ObjectLambdaCfnAccessPoint(this, "S3ObjectLambdaAccessPoint", new S3ObjectLambdaCfnAccessPointProps
            {
                Name = "transformxml",
                ObjectLambdaConfiguration = new S3ObjectLambdaCfnAccessPoint.ObjectLambdaConfigurationProperty()
                {
                    CloudWatchMetricsEnabled = true,

                    SupportingAccessPoint = supportingAccessPoint,

                    TransformationConfigurations = new object[]
                    {
                        new S3ObjectLambdaCfnAccessPoint.TransformationConfigurationProperty()
                        {
                            Actions = new string[] { "GetObject" },
                            
                            ContentTransformation = new Dictionary<string, object>()
                            {
                                { 
                                    "AwsLambda", new Dictionary<string, string>()
                                    {
                                        {"FunctionArn", function.FunctionArn }
                                    } 
                                }
                            }
                        }
                    }
                }
            });
            
            var output = new CfnOutput(this, "S3ObjectLambdaURL", new CfnOutputProps
            {
                Value = FormatArn(new ArnComponents()
                {
                    Service = "s3-object-lambda",
                    Resource = $"accesspoint/{objectLambdaAccessPoint.Name}"
                })
            });

        }
    }
}
