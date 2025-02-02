﻿using System;
using System.Text;

namespace MultiFactor.ADFS.Adapter.Services
{
    /// <summary>
    /// Service to load public key and verify token signature, issuer and expiration date
    /// </summary>
    public class TokenValidationService
    {
        private MultiFactorConfiguration _configuration;

        public TokenValidationService(MultiFactorConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Verify JWT
        /// </summary>
        public string VerifyToken(string jwt)
        {
            //https://multifactor.ru/docs/integration/

            if (string.IsNullOrEmpty(jwt))
            {
                throw new ArgumentNullException(nameof(jwt));
            }

            var parts = jwt.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var head = parts[0];
            var body = parts[1];
            var sign = parts[2];

            //validate JwtHS256 token signature
            var key = Encoding.UTF8.GetBytes(_configuration.ApiSecret);
            var message = Encoding.UTF8.GetBytes($"{head}.{body}");

            var computedSign = Util.Base64UrlEncode(Util.HMACSHA256(key, message));

            if (computedSign != sign)
            {
                throw new Exception("Invalid token signature");
            }

            var decodedBody = Encoding.UTF8.GetString(Util.Base64UrlDecode(body));
            var json = Util.JsonToDictionary(decodedBody);

            //validate audience
            var aud = json["aud"] as string;
            if (aud != _configuration.ApiKey)
            {
                throw new Exception("Invalid token audience");
            }

            //validate expiration date
            var iat = Convert.ToInt64(json["exp"]);
            if (Util.UnixTimeStampToDateTime(iat) < DateTime.UtcNow)
            {
                throw new Exception("Expired token");
            }

            //identity
            var sub = json["sub"] as string;
            if (string.IsNullOrEmpty(sub))
            {
                throw new Exception("Name ID not found");
            }

            return sub;
        }

        /// <summary>
        /// Verify JWT safe
        /// </summary>
        public bool TryVerifyToken(string jwt, out string identity)
        {
            try
            {
                identity = VerifyToken(jwt);
                return true;
            }
            catch
            {
                identity = null;
                return false;
            }
        }
    }
}
