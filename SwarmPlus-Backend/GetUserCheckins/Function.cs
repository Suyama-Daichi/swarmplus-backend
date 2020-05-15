using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using GetUserCheckins.models;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GetUserCheckins
{
    public class Function
    {
        HttpClient client;
        public Function()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("https://api.foursquare.com/v2/");
            client.DefaultRequestHeaders.Add("Acccept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Language", "ja");
        }
        /// <summary>
        /// ���[�U�[�̃`�F�b�N�C�����擾����
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<FoursquareResponse> FunctionHandler(Request input, ILambdaContext context)
        {
            string accessToken = input.headers.Authorization.Substring(7);
            string afterTimestamp = input.param.afterTimestamp;

            var response = await client.GetAsync(
    $"users/self/checkins?oauth_token={accessToken}&v=20180815&limit=250&afterTimestamp={input.param.afterTimestamp}&beforeTimestamp={input.param.beforeTimestamp}");
            var result = await response.Content.ReadAsStringAsync();
            var deserialisedResult = JsonConvert.DeserializeObject<ResponseFromFoursquare>(result);

            // 1���N�G�X�g250�`�F�b�N�C�������̑Ή�
            if (deserialisedResult.response.checkins.items.Length == 250)
            {
                CheckinInfo[] additionalCheckins = await getCheckinForOver250PerMonth(accessToken, afterTimestamp, deserialisedResult.response.checkins.items.Last().createdAt);
                deserialisedResult.response.checkins.items = deserialisedResult.response.checkins.items.Concat(additionalCheckins).ToArray();
                while (additionalCheckins.Length == 250)
                {
                    additionalCheckins = await getCheckinForOver250PerMonth(accessToken, afterTimestamp, deserialisedResult.response.checkins.items.Last().createdAt);
                    deserialisedResult.response.checkins.items = deserialisedResult.response.checkins.items.Concat(additionalCheckins).ToArray();
                }
            }

            return new FoursquareResponse
            {
                checkins = new Items
                {
                    count = deserialisedResult.response.checkins.count,
                    items = deserialisedResult.response.checkins.items
                }
            };
        }

        /// <summary>
        /// 250�`�F�b�N�C��/������ꍇ�̏���
        /// </summary>
        /// <param name="accessToken">�A�N�Z�X�g�[�N��</param>
        /// <param name="afterTimestamp">�擾�������(�n�܂�)</param>
        /// <param name="beforeTimestamp">�擾�������(�I���)</param>
        /// <param name="deserialisedResult">�r���܂ł̃`�F�b�N�C�����</param>
        /// <returns>�������ꂽ�`�F�b�N�C�����</returns>
            private async Task<CheckinInfo[]> getCheckinForOver250PerMonth(string accessToken, string afterTimestamp, int beforeTimestamp)
            {
                HttpResponseMessage moreResponse = await client.GetAsync(
                $"users/self/checkins?oauth_token={accessToken}&v=20180815&limit=250&afterTimestamp={afterTimestamp}&beforeTimestamp={beforeTimestamp}");
                string moreResult = await moreResponse.Content.ReadAsStringAsync();
                ResponseFromFoursquare moreDeserialisedResult = JsonConvert.DeserializeObject<ResponseFromFoursquare>(moreResult);
                return moreDeserialisedResult.response.checkins.items;
            }
        }
    }
