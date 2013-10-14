﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IdentityModel.Protocols.WSTrust;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace System.IdentityModel.Tokens
{
    /// <summary>
    /// A <see cref="SecurityToken"/> designed for representing a JSON Web Token (JWT).
    /// </summary>
    public class JwtSecurityToken : SecurityToken
    {
        private JwtHeader _header;
        private string _id;
        private JwtPayload _payload;
        private string _encodedToken;
        private string _signature = string.Empty;

        /// <summary>
        /// Constructs a <see cref="JwtSecurityToken"/> from a string in JWS Compact serialized format.
        /// </summary>
        /// <param name="jwtEncodedString">A JSON Web Token that has been serialized in JWS Compact serialized format.</param>
        /// <exception cref="ArgumentNullException">'jwtEncodedString' is null.</exception>
        /// <exception cref="ArgumentException">'jwtEncodedString' contains only whitespace.</exception>
        /// <exception cref="ArgumentException">'jwtEncodedString' is not in JWS Compact serialized format.</exception>
        /// <remarks>
        /// The contents of this <see cref="JwtSecurityToken"/> have not been validated, the JSON Web Token is simply decoded. Validation can be accomplished using <see cref="JwtSecurityTokenHandler.ValidateToken(SecurityToken)"/>
        /// </remarks>>
        public JwtSecurityToken( string jwtEncodedString )
        {
            if ( null == jwtEncodedString )
            {
                throw new ArgumentNullException( "jwtEncodedString" );
            }

            if ( string.IsNullOrWhiteSpace( jwtEncodedString ) )
            {
                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, WifExtensionsErrors.WIF10002, "jwtEncodedString" ) );
            }

            if ( !Regex.IsMatch( jwtEncodedString, JwtSecurityTokenHandler.JsonCompactSerializationRegex ) )
            {
                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, JwtErrors.Jwt10400, "jwtEncodedString", jwtEncodedString ) );
            }

            Decode( jwtEncodedString );
        }

        /// <summary>
        /// Constructs a <see cref="JwtSecurityToken"/> where the <see cref="JwtHeader"/> contains the crypto algorithms applied to the encoded <see cref="JwtHeader"/> and <see cref="JwtPayload"/>. The jwtEncodedString is the result of those operations.
        /// </summary>
        /// <param name="header">Contains JSON objects representing the cryptographic operations applied to the JWT and optionally any additional properties of the JWT</param>
        /// <param name="payload">Contains JSON objects representing the claims contained in the JWT. Each claim is a JSON object of the form { Name, Value }</param>
        /// <param name="jwtEncodedString">The results of encoding and applying the cryptographic operations to the <see cref="JwtHeader"/> and <see cref="JwtPayload"/>.</param>
        /// <exception cref="ArgumentNullException">'header' is null.</exception>        
        /// <exception cref="ArgumentNullException">'payload' is null.</exception>
        /// <exception cref="ArgumentNullException">'jwtEncodedString' is null.</exception>        
        /// <exception cref="ArgumentException">'jwtEncodedString' contains only whitespace.</exception>        
        /// <exception cref="ArgumentException">'jwtEncodedString' is not in JWS Compact serialized format.</exception>
        public JwtSecurityToken( JwtHeader header, JwtPayload payload, string jwtEncodedString )
        {
            if ( header == null )
            {
                throw new ArgumentNullException( "header" );
            }

            if ( payload == null )
            {
                throw new ArgumentNullException( "payload" );
            }

            if ( jwtEncodedString == null )
            {
                throw new ArgumentNullException( "jwtEncodedString" );
            }

            if ( string.IsNullOrWhiteSpace( jwtEncodedString ) )
            {
                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, WifExtensionsErrors.WIF10002, "jwtEncodedString" ) );
            }

            if (!Regex.IsMatch( jwtEncodedString, JwtSecurityTokenHandler.JsonCompactSerializationRegex ) )
            {
                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, JwtErrors.Jwt10400, "jwtEncodedString", jwtEncodedString ) );
            }
            
            string[] tokenParts = jwtEncodedString.Split( '.' );
            if ( tokenParts.Length != 3 )
            {
                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, JwtErrors.Jwt10400, "jwtEncodedString", jwtEncodedString ) );
            }

            _header = header;
            _payload = payload;
            _encodedToken = jwtEncodedString;
            _signature = tokenParts[2];
        }

        /// <summary>
        /// Constructs a <see cref="JwtSecurityToken"/> specifying optional parameters.
        /// </summary>
        /// <param name="issuer">if this value is not null, a { iss, 'issuer' } claim will be added.</param>
        /// <param name="audience">if this value is not null, a { aud, 'audience' } claim will be added</param>
        /// <param name="claims">if this value is not null then for each <see cref="Claim"/> a { 'Claim.Type', 'Claim.Value' } is added. If duplicate claims are found then a { 'Claim.Type', List&lt;object> } will be created to contain the duplicate values.</param>
        /// <param name="lifetime">if this value is not null, then if <para><see cref="Lifetime" />.Created.HasValue a { nbf, 'value' } is added.</para><para>if <see cref="Lifetime"/>.Expires.HasValue a { exp, 'value' } claim is added.</para></param>
        /// <param name="signingCredentials">The <see cref="SigningCredentials"/> that will be or was used to sign the <see cref="JwtSecurityToken"/>. See <see cref="JwtHeader(SigningCredentials)"/> for details pertaining to the Header Parameter(s).</param>
        public JwtSecurityToken( string issuer = null, string audience = null, IEnumerable<Claim> claims = null, Lifetime lifetime = null, SigningCredentials signingCredentials = null )
        {
            _payload = new JwtPayload( issuer, audience, claims, lifetime );
            _header  = new JwtHeader( signingCredentials );
        }

        /// <summary>
        /// Decodes the string into the header, payload and signature
        /// </summary>
        /// <param name="jwtEncodedString">Base64Url encoded string.</param>
        internal void Decode( string jwtEncodedString )
        {
            string[] tokenParts = jwtEncodedString.Split( '.' );
            if ( tokenParts.Length != 3 )
            {
                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, JwtErrors.Jwt10400, "jwtEncodedString", jwtEncodedString ) );
            }

            try
            {
                _header = Base64UrlEncoder.Decode( tokenParts[0] ).DeserializeJwtHeader();
                
                // if present, "typ" should be set to "JWT" or "http://openid.net/specs/jwt/1.0"
                string type = null;
                if ( _header.TryGetValue( JwtConstants.ReservedHeaderParameters.Type, out type ) )
                {
                    if ( !( StringComparer.Ordinal.Equals( type, JwtConstants.HeaderType ) || StringComparer.Ordinal.Equals( type, JwtConstants.HeaderTypeAlt ) ) )
                    {
                        throw new SecurityTokenException( string.Format( CultureInfo.InvariantCulture, JwtErrors.Jwt10112, JwtConstants.HeaderType, JwtConstants.HeaderTypeAlt, type ) );
                    }
                }
            }
            catch ( Exception ex )
            {
                if ( DiagnosticUtility.IsFatal( ex ) )
                {
                    throw;
                }

                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, JwtErrors.Jwt10113, "header", tokenParts[0], jwtEncodedString ), ex );
            }

            try
            {
                _payload = Base64UrlEncoder.Decode( tokenParts[1] ).DeserializeJwtPayload();
            }
            catch ( Exception ex )
            {
                if ( DiagnosticUtility.IsFatal( ex ) )
                {
                    throw;
                }

                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, JwtErrors.Jwt10113, "payload", tokenParts[1], jwtEncodedString ), ex );
            }

            _encodedToken = jwtEncodedString;
            _signature = tokenParts[2];
            return;
        }

        /// <summary>
        /// Gets the 'value' of the 'actor' claim { actort, 'value' }.
        /// </summary>
        /// <remarks>If the 'actor' claim is not found, null is returned.</remarks> 
        public string Actor
        {
            get { return _payload.Actor; }
        }

        /// <summary>
        /// Gets the 'value' of the 'audience' claim { aud, 'value' }.
        /// </summary>
        /// <remarks>If the 'audience' claim is not found, null is returned.</remarks>
        public string Audience
        {
            get { return _payload.Audience; }
        }

        /// <summary>
        /// Gets the <see cref="Claim"/>(s) for this token.
        /// </summary>
        /// <remarks><para><see cref="Claim"/>(s) returned will NOT have the <see cref="Claim.Type"/> translated according to <see cref="JwtSecurityTokenHandler.InboundClaimTypeMap"/></para></remarks>
        public IEnumerable<Claim> Claims
        {
            get { return _payload.Claims; }
        }

        /// <summary>
        /// Gets the Base64UrlEncoded <see cref="JwtHeader"/> associated with this instance.
        /// </summary>
        public string EncodedHeader
        {
            get { return _header.Encode(); }
        }

        /// <summary>
        /// Gets the Base64UrlEncoded <see cref="JwtPayload"/> associated with this instance.
        /// </summary>
        public string EncodedPayload
        {
            get { return _payload.Encode(); }
        }

        /// <summary>
        /// Gets the 'value' of the 'expiration' claim { aud, 'exp' }.
        /// </summary>
        /// <remarks>If the 'expiration' claim is not found OR could not be converted to <see cref="Int32"/>, null is returned.</remarks>
        public Int32? Expiration
        {
            get { return _payload.Expiration; }
        }

        /// <summary>
        /// Gets the <see cref="JwtHeader"/> associated with this instance.
        /// </summary>
        public JwtHeader Header
        {
            get { return _header; }
        }

        /// <summary>
        /// Gets the 'value' of the 'JWT ID' claim { jti, ''value' }.
        /// </summary>
        /// <remarks>If the 'JWT ID' claim is not found, null is returned.</remarks>
        public override string Id
        {
            get { return _payload.Id; }
        }

        /// <summary>
        /// Gets the 'value' of the 'Issued At' claim { iat, 'value' }.
        /// </summary>
        /// <remarks>If the 'Issued At' claim is not found OR cannot be converted to <see cref="Int32"/> null is returned.</remarks>
        public Int32? IssuedAt
        {
            get { return _payload.IssuedAt; }
        }

        /// <summary>
        /// Gets the 'value' of the 'issuer' claim { iss, 'value' }.
        /// </summary>
        /// <remarks>If the 'issuer' claim is not found, null is returned.</remarks>
        public string Issuer
        {
            get { return _payload.Issuer; }
        }

        /// <summary>
        /// Gets the <see cref="JwtPayload"/> associated with this instance.
        /// </summary>
        public JwtPayload Payload
        {
            get { return _payload; }
        }
        
        /// <summary>
        /// Gets the original raw data of this instance when it was created.
        /// </summary>
        /// <remarks>The original JSON Compact serialized format passed to one of the two constructors <see cref="JwtSecurityToken(string)"/> 
        /// or <see cref="JwtSecurityToken( JwtHeader, JwtPayload, string )"/></remarks>
        public string RawData
        {
            get { return _encodedToken; }
        }

        /// <summary>
        /// Gets the current signature over the jwt
        /// </summary>
        public string EncodedSignature
        {
            get { return _signature; }
        }

        /// <summary>
        /// Gets the <see cref="SecurityKey"/>s for this instance.
        /// </summary>
        /// <remarks>By default an empty collection is returned.</remarks>
        public override ReadOnlyCollection<SecurityKey> SecurityKeys
        {
            get { return new ReadOnlyCollection<SecurityKey>( new List<SecurityKey>() ); }
        }

        /// <summary>
        /// Gets the signature algorithm associated with this instance.
        /// </summary>
        /// <remarks>if there is a <see cref="SigningCredentials"/> associated with this instance, a value will be returned.  Null otherwise.</remarks>
        public string SignatureAlgorithm
        {
            get { return _header.SignatureAlgorithm; }
        }

        /// <summary>
        /// Gets the <see cref="SigningCredentials"/> associated with this instance.
        /// </summary>
        public SigningCredentials SigningCredentials
        {
            get { return _header.SigningCredentials; }
        }

        /// <summary>
        /// Gets or sets the <see cref="SecurityKey"/> that signed this instance.
        /// </summary>
        /// <remarks><see cref="JwtSecurityTokenHandler"/>.ValidateSignature(...) sets this value when a <see cref="SecurityKey"/> is used to successfully validate a signature.</remarks>
        public SecurityKey SigningKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="SecurityToken"/> that contains a <see cref="SecurityKey"/> that signed this instance.
        /// </summary>
        /// <remarks><see cref="JwtSecurityTokenHandler"/>.ValidateSignature(...) sets this value when a <see cref="SecurityKey"/> is used to successfully validate a signature.</remarks>
        public SecurityToken SigningToken
        {
            get;
            set;
        }

        internal void SetId( string id )
        {
            _id = id;
        }

        /// <summary>
        /// Gets "value" of the 'subject' claim { sub, 'value' }.
        /// </summary>
        /// <remarks>If the 'subject' claim is not found, null is returned.</remarks>
        public string Subject
        {
            get
            {
                return _payload.Subject;
            }
        }

        /// <summary>
        /// Gets 'value' of the 'notbefore' claim { nbf, 'value' } converted to a <see cref="DateTime"/> assuming 'value' is seconds since UnixEpoch (UTC 1970-01-01T0:0:0Z).
        /// </summary>
        /// <remarks>If the 'notbefore' claim is not found, then <see cref="DateTime.MinValue"/> is returned.</remarks>
        public override DateTime ValidFrom
        {
            get { return _payload.ValidFrom; }
        }

        /// <summary>
        /// Gets 'value' of the 'expiration' claim { exp, 'value' } converted to a <see cref="DateTime"/> assuming 'value' is seconds since UnixEpoch (UTC 1970-01-01T0:0:0Z).
        /// </summary>
        /// <remarks>If the 'expiration' claim is not found, then <see cref="DateTime.MinValue"/> is returned.</remarks>
        public override DateTime ValidTo
        {
            get { return _payload.ValidTo; }
        }

        /// <summary>
        /// Decodes the <see cref="JwtHeader"/> and <see cref="JwtPayload"/>
        /// </summary>
        /// <returns>A string containing the header and payload in JSON format</returns>
        public override string ToString()
        {
            return string.Format( CultureInfo.InvariantCulture, "{0}.{1}", _header.SerializeToJson(), _payload.SerializeToJson() );
        }
    }
}
