using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class WizzairTests
{
    private IWebDriver driver;
    private const string BaseURL = "https://wizzair.com";

    [SetUp]
    public void SetupTest()
    {
        var options = new ChromeOptions();
        options.AddArgument("--lang=en");
        options.DebuggerAddress = "localhost:9222";

        driver = new ChromeDriver(options);
        driver.Manage().Window.Maximize();
    }

    private void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver?.Quit();
            driver?.Dispose();
        }
        catch { }
    }

    [Test]
    public void Wizzair_NextWeek_BucharestBudapest_ShouldHaveTwoFlights()
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
        driver.Navigate().GoToUrl(BaseURL);

        // Cookie
        try
        {
            var cookieButton = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.CssSelector("[data-test='cookie-accept-all']")));
            cookieButton.Click();
        }
        catch { }

        // Budapest
        var origin = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("[data-test='search-departure-station']")));
        origin.Clear();
        origin.SendKeys("Budapest");
        Thread.Sleep(1500);

        var budapestOption = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("[aria-label='Budapest']")));
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", budapestOption);

        // Bucharest Otopeni
        var destination = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("[data-test='search-arrival-station']")));
        destination.Clear();
        destination.SendKeys("Bucharest");
        Thread.Sleep(1500);

        var bucharestOption = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("[data-test='OTP']")));
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", bucharestOption);

        Thread.Sleep(1000);

        // Kovetkezo het
        DateTime today = DateTime.Now;
        int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilNextMonday == 0) daysUntilNextMonday = 7;
        DateTime nextMonday = today.AddDays(daysUntilNextMonday);

        // Departure: kvoetkezo het elso elerheto napja
        var dateCell = wait.Until(d =>
        {
            var enabledCells = d.FindElements(By.XPath(
                "//span[contains(@class,'vc-day-content') and not(contains(@class,'is-disabled'))]"));
            foreach (var cell in enabledCells)
            {
                var label = cell.GetAttribute("aria-label");
                if (DateTime.TryParse(label, out var cellDate) && cellDate >= nextMonday)
                    return cell;
            }
            return enabledCells.Count > 0 ? enabledCells[0] : null;
        });
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", dateCell);

        // Return: kovetkezo elerheto nap
        Thread.Sleep(500);
        var returnCell = wait.Until(d =>
        {
            var enabledCells = d.FindElements(By.XPath(
                "//span[contains(@class,'vc-day-content') and not(contains(@class,'is-disabled'))]"));
            return enabledCells.Count > 4 ? enabledCells[4] :
                   enabledCells.Count > 0 ? enabledCells[enabledCells.Count - 1] : null;
        });
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", returnCell);

        Thread.Sleep(500);
        var searchButton = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("[data-test='flight-search-submit']")));
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", searchButton);

        // Legalabb 2 jarat
        var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
        var flights = wait2.Until(d =>
        {
            var els = d.FindElements(By.CssSelector("[data-test='flight-card']"));
            return els.Count >= 2 ? els : null;
        });

        flights.Count.Should().BeGreaterThanOrEqualTo(2);
    }
}