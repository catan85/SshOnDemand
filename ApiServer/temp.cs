//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Runtime.Caching;
//using System.Security.Cryptography;
//using System.Security.Principal;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Http.Filters;
//using System.Web.Http.Results;

//namespace HMACAuthenticationWebApi.Models
//{
//    public class HMACAuthenticationAttribute : Attribute, IAuthenticationFilter
//    {
//        private static Dictionary<string, string> allowedApps = new Dictionary<string, string>();
//        private readonly UInt64 requestMaxAgeInSeconds = 300; //Means 5 min
//        private readonly string authenticationScheme = "hmacauth";

//        public HMACAuthenticationAttribute()
//        {
//            if (allowedApps.Count == 0)
//            {
//                allowedApps.Add("65d3a4f0-0239-404c-8394-21b94ff50604", "WLUEWeL3so2hdHhHM5ZYnvzsOUBzSGH4+T3EgrQ91KI=");
//            }
//        }

//        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
//        {
//            var req = context.Request;

//            if (req.Headers.Authorization != null && authenticationScheme.Equals(req.Headers.Authorization.Scheme, StringComparison.OrdinalIgnoreCase))
//            {
//                var rawAuthzHeader = req.Headers.Authorization.Parameter;

//                var autherizationHeaderArray = GetAutherizationHeaderValues(rawAuthzHeader);

//                if (autherizationHeaderArray != null)
//                {
//                    var APPId = autherizationHeaderArray[0];
//                    var incomingBase64Signature = autherizationHeaderArray[1];
//                    var nonce = autherizationHeaderArray[2];
//                    var requestTimeStamp = autherizationHeaderArray[3];

//                    var isValid = IsValidRequest(req, APPId, incomingBase64Signature, nonce, requestTimeStamp);

//                    if (isValid.Result)
//                    {
//                        var currentPrincipal = new GenericPrincipal(new GenericIdentity(APPId), null);
//                        context.Principal = currentPrincipal;
//                    }
//                    else
//                    {
//                        context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
//                    }
//                }
//                else
//                {
//                    context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
//                }
//            }
//            else
//            {
//                context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
//            }

//            return Task.FromResult(0);
//        }

//        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
//        {
//            context.Result = new ResultWithChallenge(context.Result);
//            return Task.FromResult(0);
//        }

//        public bool AllowMultiple
//        {
//            get { return false; }
//        }

//        private string[] GetAutherizationHeaderValues(string rawAuthzHeader)
//        {

//            var credArray = rawAuthzHeader.Split(':');

//            if (credArray.Length == 4)
//            {
//                return credArray;
//            }
//            else
//            {
//                return null;
//            }
//        }
//    }
//}