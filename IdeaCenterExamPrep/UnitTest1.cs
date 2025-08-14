using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using ExamPrep1.Models;
using System.ComponentModel.Design;


namespace ExamPrep1;

[TestFixture]

public class IdeaCenterApiTests
{
    private RestClient client;
    private static string? lastCreatedIdeaId;

    private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

    private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJmYTFkODE1Zi04NTdjLTRlZjUtOGI4ZS1jYzBlZDY3NTUzNmIiLCJpYXQiOiIwOC8xMy8yMDI1IDEzOjQ3OjAwIiwiVXNlcklkIjoiYjM2OTMzN2UtYzUyMS00MDUyLWQyYTQtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJleGFtcHJlcDFuaW5hQGV4YW1wbGUuY29tIiwiVXNlck5hbWUiOiJleGFtcHJlcDFuaW5hIiwiZXhwIjoxNzU1MTE0NDIwLCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.UPqbLSqUYsJSTm7cIJzJghaDa0u5BjuaQbo0e9f86Wc";

    private const string LoginEmail = "examprep1nina@example.com";
    private const string LoginPassword = "examprep1nina";

    [OneTimeSetUp]
    public void Setup()
    {
        string jwtToken;

        if (!string.IsNullOrWhiteSpace(StaticToken))
        {
            jwtToken = StaticToken;
        }
        else
        {
            jwtToken = GetJwtToken(LoginEmail, LoginPassword);
        }

        var options = new RestClientOptions(BaseUrl)
        {
            Authenticator = new JwtAuthenticator(jwtToken),
        };

        this.client = new RestClient(options);
    }

    private string GetJwtToken(string email, string password)
    {
        var tempClient = new RestClient(BaseUrl);
        var request = new RestRequest("/api/user/Authentication", Method.Post);
        request.AddBody(new { email, password });

        var response = tempClient.Execute(request);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var token = content.GetProperty("accessToken").GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Failed to retrieve JWT token from the response");
            }
            return token;

        }
        else
        {
            throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
        }
    }

    [Order(1)]
    [Test]
    public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
    {
        var ideaRequest = new IdeaDTO
        {
            Title = "Test Idea",
            Description = "This is a test idea description",
            Url = ""
        };

        var request = new RestRequest("/api/Idea/Create", Method.Post);
        request.AddJsonBody(ideaRequest);
        var response = this.client.Execute(request);
        var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
        lastCreatedIdeaId = createResponse.Id;


        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));

        Console.WriteLine("Raw Create Response: " + response.Content);
    }

    [Order(2)]
    [Test]
    public void GetAllIdeas_ShouldReturnListOfIdeas()
    {
        var request = new RestRequest("/api/Idea/All", Method.Get);
        var response = this.client.Execute(request);

        var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseItems, Is.Not.Null);
        Assert.That(responseItems, Is.Not.Empty);

        lastCreatedIdeaId = responseItems.LastOrDefault()?.Id;
    }

    [Order(3)]
    [Test]
    public void EditExistingIdea_ShouldReturnSuccess()
    {
        var editRequest = new IdeaDTO
        {
            Title = "Edited Idea",
            Description = "This is an edited idea description",
            Url = ""
        };

        var request = new RestRequest($"/api/Idea/Edit", Method.Put);
        request.AddQueryParameter("ideaId", lastCreatedIdeaId);
        request.AddJsonBody(editRequest);
        var response = this.client.Execute(request);
        var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(editResponse.Msg, Is.EqualTo("Successfully edited"));
    }

    [Order(4)]
    [Test]
    public void DeleteIdea_ShouldReturnSuccess()
    {

        var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
        request.AddQueryParameter("ideaId", lastCreatedIdeaId);
        var response = this.client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Does.Contain("The idea is deleted!"));
    }

    [Order(5)]
    [Test]
    public void CreateIdea_WithoutRequiredFields_ShouldReturnSuccess()
    {
        var ideaRequest = new IdeaDTO
        {
            Title = "",
            Description = "",
            Url = ""
        };

        var request = new RestRequest("/api/Idea/Create", Method.Post);
        request.AddJsonBody(ideaRequest);
        var response = this.client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Order(6)]
    [Test]
    public void EditNonExistingIdea_ShouldReturnSuccess()
    {
        string nonExistingIdea = "123";
        var editRequest = new IdeaDTO
        {
            Title = "Edit Non-Existing idea",
            Description = "This is an updated test on editing a non-existing idea",
            Url = ""
        };

        var request = new RestRequest($"/api/Idea/Edit", Method.Put);
        request.AddQueryParameter("ideaId", nonExistingIdea);
        request.AddJsonBody(editRequest);
        var response = this.client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content, Does.Contain("There is no such idea!"));
    }

    [Order(7)]
    [Test]
    public void DeleteNonExistingIdea_ShouldReturnSuccess()
    {
        string nonExistingIdea = "123";

        var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
        request.AddQueryParameter("ideaId", nonExistingIdea);
        var response = this.client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content, Does.Contain("There is no such idea!"));
    }

    [OneTimeTearDown]

    public void TearDown()
    {
        this.client?.Dispose();
    }
}

