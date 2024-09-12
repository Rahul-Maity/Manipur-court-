using System;
using System.Drawing;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Patagames.Ocr.Enums;
using Patagames.Ocr;
using Tesseract;
using IronOcr;
using OpenQA.Selenium.Support.UI;


namespace CaptchaBypass
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize the Chrome WebDriver
            using (IWebDriver driver = new ChromeDriver())
            {
                string url = "https://hcservices.ecourts.gov.in/ecourtindiaHC/cases/s_orderdate.php?state_cd=25&dist_cd=1&court_code=1&stateNm=Manipur";
                int maxRetries = 3;
                int retryCount = 0;
                bool success = false;

                while (retryCount < maxRetries && !success)
                {
                    try
                    {
                        driver.Navigate().GoToUrl(url);
                        success = true;
                    }
                    catch (WebDriverException ex)
                    {
                        Console.WriteLine($"Error navigating to URL: {ex.Message}");
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            Console.WriteLine("Retrying...");
                            System.Threading.Thread.Sleep(3000); // Wait for 3 seconds before retrying
                        }
                        else
                        {
                            Console.WriteLine("Max retries reached. Exiting...");
                        }
                    }
                }

                if (success)
                {
                    DateTime today = DateTime.Today;
                    DateTime oneMonthAgo = today.AddMonths(-1);

                    // Format dates as DD-MM-YYYY
                    string fromDate = oneMonthAgo.ToString("dd-MM-yyyy");
                    string toDate = today.ToString("dd-MM-yyyy");

                    IWebElement fromDateElement = driver.FindElement(By.Id("from_date")); // Adjust ID as necessary
                    IWebElement toDateElement = driver.FindElement(By.Id("to_date"));     // Adjust ID as necessary

                  

                    fromDateElement.SendKeys(fromDate);
                    toDateElement.SendKeys(toDate);


                    IWebElement captchaImageElement = driver.FindElement(By.XPath("//img[@id='captcha_image']"));

                    // Get the source URL of the CAPTCHA image
                    string captchaImageUrl = captchaImageElement.GetAttribute("src");

                    // Define the path for saving the CAPTCHA image
                    string projectDirectory = Directory.GetCurrentDirectory(); // Gets the current project directory
                    string imgFolderPath = Path.Combine(projectDirectory, "img");

                    // Create the directory if it does not exist
                    if (!Directory.Exists(imgFolderPath))
                    {
                        Directory.CreateDirectory(imgFolderPath);
                    }

                    string captchaImagePath = Path.Combine(imgFolderPath, "captcha.png");


                   // string captchaImagePath = Path.Combine(imgFolderPath, "captcha.png");
                    DownloadImage(captchaImageUrl, captchaImagePath);


                    string captchaText;

                    // Path for the CAPTCHA image
                    IronOcr.Installation.LicenseKey = "IRONSUITE.MRAHULMAITY623.GMAIL.COM.12335-1F5EEA635C-BPTWFAUNRRPSSRO3-5V25FKOEAE6V-WRWDOF3DRN47-B23U7XPTL7EK-GEIHSYT2MVSX-YN35AAPIDKRR-G6TNWO-T2AFOTRU5JGNUA-DEPLOYMENT.TRIAL-RUBHQB.TRIAL.EXPIRES.12.OCT.2024";
                    var ocr = new IronTesseract();
                    using (var input = new OcrInput(captchaImagePath))
                    {
                        // Preprocessing the image to enhance OCR accuracy
                        input.DeNoise();  // Remove noise
                        input.Invert();   // Invert image if it's dark text on light background
                        input.EnhanceResolution();// Enhance contrast for better clarity

                        // Process the image
                        OcrResult result = ocr.Read(input);

                        // Output the recognized text
                        Console.WriteLine(result.Text);
                        captchaText = result.Text;
                    }


                    


                    driver.FindElement(By.Id("captcha")).SendKeys(captchaText);

                    IWebElement element = driver.FindElement(By.Name("submit1"));
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                    element.Click();





                    System.Threading.Thread.Sleep(5000); // 5 seconds
                }
               
            }
        }

      

        private static string ConvertImageToText(string captchaImagePath)
        {
            string plainText = "";
            string dllPath = @"x64\tesseract.dll";
            if (File.Exists(dllPath))
            {
                OcrApi.PathToEngine = dllPath;
                using (var api = OcrApi.Create())
                {
                    api.Init(Languages.English);
                    plainText = api.GetTextFromImage(captchaImagePath);
                    Console.WriteLine(plainText);
                }
            }
            return plainText;

        }

        private static string GetText(Bitmap imgsource)
        {
            string ocrtext = string.Empty;
            try
            {
                // Ensure the path to the tessdata directory is correct
                string tessdataPath = @"C:\Program Files\Tesseract-OCR\tessdata";

                using (var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default))
                {
                    using (var img = PixConverter.ToPix(imgsource))
                    {
                        using (var page = engine.Process(img))
                        {
                            ocrtext = page.GetText().Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return ocrtext;
        }

        // Method to download CAPTCHA image from the URL
        static void DownloadImage(string imageUrl, string savePath)
        {
            try
            {
                using (var webClient = new System.Net.WebClient())
                {
                    // Download the image from the URL
                    webClient.DownloadFile(imageUrl, savePath);
                    Console.WriteLine($"Image downloaded successfully: {savePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
            }
        }


     

        // Method to extract text from the CAPTCHA image using Tesseract OCR
        static string ReadCaptchaWithTesseract(string imagePath)
        {
            try
            {
                // Verify the image file exists
                if (!File.Exists(imagePath))
                {
                    throw new FileNotFoundException("Captcha image file not found", imagePath);
                }

                // Use the absolute path to the directory containing `tessdata`
                string tessdataPath = @"C:\Program Files\Tesseract-OCR\tessdata";

                // Initialize the Tesseract OCR engine
                using (var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default))
                {
                    // Load the CAPTCHA image
                    using (var img = Pix.LoadFromFile(imagePath))
                    {
                        // Optionally, apply preprocessing if necessary
                        // Example: Convert image to grayscale
                        using (var processedImg = PreprocessImage(img))
                        {
                            // Process the image with OCR
                            using (var page = engine.Process(processedImg))
                            {
                                // Get the extracted text from the image
                                string text = page.GetText().Trim();
                                return text;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return string.Empty;
            }
        }

        // Optional method for image preprocessing
        static Pix PreprocessImage(Pix img)
        {
            // Example preprocessing: Convert to grayscale
            var grayscaleImg = img.ConvertRGBToGray();
            return grayscaleImg;
        }


    }
}


