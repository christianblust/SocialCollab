namespace LuisBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;
    using System.Text;
    using System.Net.Http;
    using System.Net.Http.Headers;

    [Serializable]
    [LuisModel("80606d5f-8d1a-4220-8f21-da9b3f01961a", "c4499f06fe4e426bada0c39a17a6164b")]
    public class RootLuisDialog : LuisDialog<object>
    {
        private const string EntityChannel = "channel";
        private const string EntityVolume = "volume";

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            // Check for empty query
            var message = await item;
            if (message.Text == null)
            {
                // Return the Help/Welcome
                await Help(context, null);
            }
            else
            {
                await base.MessageReceived(context, item);
            }
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            var response = context.MakeMessage();
            response.Text = $"Sorry, I did not understand '{result.Query}'. Use 'help' if you need assistance.";
            response.Speak = $"Sorry, I did not understand '{result.Query}'. Say 'help' if you need assistance.";
            response.InputHint = InputHints.AcceptingInput;

            await context.PostAsync(response);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            var response = context.MakeMessage();
            response.Summary = "Hi! Try asking me things like 'turn on/off TV', 'change channel' or 'set volume'";
            response.Speak = "Hi! Try asking me things like 'turn on/off TV', 'change channel' or 'set volume'";
            response.Text = "We are here to help, what can I do for you?";
            response.InputHint = InputHints.ExpectingInput;

            await context.PostAsync(response);
     
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("TurnOffTV")]
        [LuisIntent("TurnOnTV")]
        public async Task TurnOnOffTV(IDialogContext context, LuisResult result)
        {
            IntentRecommendation recommendation;
            recommendation = result.TopScoringIntent;

            String currentIntent = "";
            String queryPara = "";

            currentIntent = recommendation.Intent;

            if (currentIntent.Equals("TurnOnTV")) queryPara = "1";
            else queryPara = "0";

            //Initialize Rest Client and make the request
            var client = new HttpClient();

            client.BaseAddress = new Uri("https://socialcollabapi.azurewebsites.net/");
 
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage responseMessage = await client.GetAsync("api/Tv?parameter="+ queryPara);

            string apiResponse = "";

            if (responseMessage.IsSuccessStatusCode)
            {
                 apiResponse = await responseMessage.Content.ReadAsStringAsync();       
            }

            var response = context.MakeMessage();
            response.Summary = "This is the " + currentIntent + " Intent";
            response.Speak = apiResponse;
            response.Text = apiResponse;
            response.InputHint = InputHints.ExpectingInput;

            await context.PostAsync(response);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("ChangeChannelSetVolume")]
        public async Task ChangeChannelSetVolume(IDialogContext context, LuisResult result)
        {
            var response = context.MakeMessage();

            //Initialize EntityRecommendations
            EntityRecommendation channelEntityRecommendation;
            EntityRecommendation volumeEntityRecommendation;

            IList<EntityRecommendation> entities = result.Entities;

            Tv tv = new Tv(null, null, null);

            // if both entities are recognized, make API call to set both values
            if (result.TryFindEntity(EntityChannel, out channelEntityRecommendation) && result.TryFindEntity(EntityVolume, out volumeEntityRecommendation))
            {
                tv = new Tv(null, channelEntityRecommendation.Entity, volumeEntityRecommendation.Entity);
            }

            // if we just recognize volume, make API call to set volume only
            else if (result.TryFindEntity(EntityVolume, out volumeEntityRecommendation))
            {
                tv = new Tv(null, null, volumeEntityRecommendation.Entity);
            }

            // if we just recognize channel, make API call to set channel only
            else if (result.TryFindEntity(EntityChannel, out channelEntityRecommendation))
            {
                tv = new Tv(null, channelEntityRecommendation.Entity, null);
            }

            // Make REST Call depending on given parameters to set channel and/or the volume
            var client = new HttpClient();

            client.BaseAddress = new Uri("https://socialcollabapi.azurewebsites.net/");

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage responseMessage = await client.PostAsJsonAsync("api/Tv", tv);
            string apiResponse = "";

            if (responseMessage.IsSuccessStatusCode)
            {
                apiResponse = await responseMessage.Content.ReadAsStringAsync();
            }

            response.Summary = apiResponse;
            response.Speak = apiResponse;
            response.Text = apiResponse;

            response.InputHint = InputHints.ExpectingInput;

            await context.PostAsync(response);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Goodbye")]
        public async Task Goodbye(IDialogContext context, LuisResult result)
        {
            var goodByeMessage = context.MakeMessage();
            goodByeMessage.Summary = goodByeMessage.Speak = "Thanks for using SocialCollab skill";
            goodByeMessage.InputHint = InputHints.IgnoringInput;

            await context.PostAsync(goodByeMessage);

            var completeMessage = context.MakeMessage();
            completeMessage.Type = ActivityTypes.EndOfConversation;
            completeMessage.AsEndOfConversationActivity().Code = EndOfConversationCodes.CompletedSuccessfully;

            await context.PostAsync(completeMessage);

            context.Done(default(object));
        }
    }

    static class StringExtensions
    {
        public static string Capitalize(this string input)
        {
            var output = string.Empty;
            if (!string.IsNullOrEmpty(input))
            {
                output = input.Substring(0, 1).ToUpper() + input.Substring(1);
            }
            // Strip out periods 
            output = output.Replace(".", "");

            return output;
        }
    }
}
