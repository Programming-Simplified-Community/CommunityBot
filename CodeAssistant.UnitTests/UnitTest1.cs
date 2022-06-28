using CodeAssistant.Services;

namespace CodeAssistant.UnitTests;

public class Tests
{
    private CodeAssistantService _service;
    
    [SetUp]
    public void Setup()
    {
        _service = new();
    }

    [Test]
    public void Test1()
    {
        var sample = @"```python
def method(a,b):
    pass
```";
        var response = CodeAssistantService.ExtractContent(sample, out var language);

        Assert.That(language, Is.EqualTo("python"));
        Assert.That(response.ToString(), Is.EqualTo("\r\ndef method(a,b):\r\n    pass\r\n"));
    }
}