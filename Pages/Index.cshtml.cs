using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Collections.Immutable;
using System.IO.Compression;


namespace ImageOptimizer.Pages
{

    public class DirectoryInfo
    {
        public string DirName { get; set; }
        public string DirPath { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _env;

        string outputPath = "wwwroot/images/optimized_image.jpg";

        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }



        [BindProperty]
        [Display(Name = "Image(s)")]
        public List<IFormFile> ImageFiles { get; set; }

        [BindProperty]
        public Boolean checkCompress { get; set; }


        [BindProperty]
        public int ImageQuality { get; set; } = 80;

        [BindProperty]
        [Display(Name = "Folder Name")]
        [Required(AllowEmptyStrings = true)]
        public string FolderName { get; set; }

        [BindProperty]
        public Boolean checkResize { get; set; }

        [BindProperty]
        [Display(Name = "Width")]
        public int ImgWidth { get; set; }

        [BindProperty]
        [Display(Name = "Height")]
        public int ImgHeight { get; set; }


        [BindProperty]
        public Boolean checkConvert { get; set; }

        public string UploadsPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");
        public IEnumerable<string> Directories { get; set; }
        public IEnumerable<string> Files { get; set; }

        public List<string> dirList { get; set; }

        [BindProperty]
        public string SelectedImageFormat { get; set; }

        public List<DirectoryInfo> dirListWithPath = new List<DirectoryInfo>();

        private static readonly string[] imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        private bool isDataLoaded = false;

        public void OnGet()
        {

           

            if (TempData.ContainsKey("ErrorMessage"))
            {
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
            }

            var dir = Request.Query["dir"];
            


            if (!String.IsNullOrEmpty(dir))
            {
                UploadsPath = Path.Combine(dir);
            }



            if (!isDataLoaded)
            {
                LoadDirectoriesAndFiles();
            }

            

            

        }

        public async Task<IActionResult> OnPostOptimize()
        {

            if (!isDataLoaded)
            {
                LoadDirectoriesAndFiles();
            }


            var uploadsFolder = Path.Combine(_env.WebRootPath, "Uploads");

            if (!string.IsNullOrEmpty(FolderName))
            {
                uploadsFolder = Path.Combine(uploadsFolder, FolderName);
            }

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }



            if (ImageFiles == null || ImageFiles.Count == 0)
            {
                ModelState.AddModelError("", "No Images Found !");
                return Page();
            }


            


            try
            {
                foreach (var ImageFile in ImageFiles)
                {
                    if (!IsImageFile(Path.GetExtension(ImageFile.FileName)))
                    {
                        ModelState.AddModelError("", "Invalid Images(s) Found!");
                        return Page();
                    }
                }

                    

                foreach (var ImageFile in ImageFiles)
                {



                    if (ImageFile == null || ImageFile.Length == 0)
                    {
                        ModelState.AddModelError("", "Invalid File(s) Found!");
                        return Page();
                    }

                    

                    using (Stream stream = ImageFile.OpenReadStream())
                    {


                        using (MagickImage image = new MagickImage(stream))
                        {

                           

                            if (!checkResize && !checkCompress && !checkConvert)
                            {
                                ModelState.AddModelError("", "Select any operation to perform !");
                                return Page();
                            }


                            if (checkConvert)
                            {

                                
                                if (SelectedImageFormat == null || SelectedImageFormat.Equals("00"))
                                {
                                    ModelState.AddModelError("", "Select Format to Convert");
                                    return Page();
                                }



                                switch (SelectedImageFormat)
                                {
                                    case "jpg":
                                        image.Format = MagickFormat.Jpeg;
                                        break;
                                    case "png":
                                        image.Format = MagickFormat.Png;
                                        break;
                                    case "gif":
                                        image.Format = MagickFormat.Gif;
                                        break;
                                    case "bmp":
                                        image.Format = MagickFormat.Bmp;
                                        break;
                                    case "tiff":
                                        image.Format = MagickFormat.Tiff;
                                        break;
                                    case "ico":
                                        image.Format = MagickFormat.Ico;
                                        break;
                                    case "pdf":
                                        image.Format = MagickFormat.Pdf;
                                        break;
                                    default:
                                        ModelState.AddModelError("", "Unsupported image format.");
                                        return Page();

                                }
                            }


                            if (checkCompress)
                            {
                                if (ImageQuality < 0)
                                {
                                    ModelState.AddModelError("", "Please select a valid compression quality.");
                                    return Page(); 
                                }

                                image.Quality = ImageQuality;
                            }

                            if (checkResize)
                            {
                                if (ImgWidth == 0 || ImgHeight == 0)
                                {
                                    ModelState.AddModelError("","Width and Height are required !");
                                    return Page();
                                }

                                var size = new MagickGeometry(ImgWidth, ImgHeight);
                                size.IgnoreAspectRatio = true;
                                image.Resize(size);


                            }


                            




                            using (MemoryStream compressedStream = new MemoryStream())
                            {
                                

                                image.Write(compressedStream);

                                compressedStream.Position = 0;


                                var fileName = Path.GetFileName(ImageFile.FileName);

                                if (checkConvert)
                                {
                                    fileName = $"{Path.GetFileNameWithoutExtension(ImageFile.FileName)}.{SelectedImageFormat}";
                                }
                                


                                var filePath = Path.Combine(uploadsFolder, fileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await compressedStream.CopyToAsync(fileStream);
                                }

                            }
                        }
                    }
                }
                return Redirect("/");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message.ToString());
                return Page();
            }

        }

        public void LoadDirectoriesAndFiles()
        {
            try
            {
                UploadsPath = UploadsPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string directoryPath = Path.GetDirectoryName(UploadsPath);



                if (directoryPath == null)
                {
                    ViewData["ErrorMessage"] = "Invalid Directory Path!";
                    RedirectToPage();
                }
                else if (!Directory.Exists(directoryPath))
                {
                    ViewData["ErrorMessage"] = "No Directory Found!";
                    RedirectToPage();
                }
                else
                {
                    dirList = directoryPath.Split(Path.DirectorySeparatorChar).ToList();
                }




                int f = 0;
                string root = "wwwroot";

                if(dirList != null && dirList.Count > 0)
                {
                    for (int i = 0; i < dirList.Count; i++)
                    {
                        if (f == 1)
                        {
                            dirListWithPath.Add(new DirectoryInfo
                            {
                                DirName = dirList[i],
                                DirPath = Path.Combine(Directory.GetCurrentDirectory(), root, dirList[i])
                            });
                            root += "/" + dirList[i];
                        }
                        if (dirList[i].ToLower() == "wwwroot")
                        {
                            f = 1;
                        }

                    }
                }
                




                Directories = Directory.GetDirectories(UploadsPath)
                              .Select(path => new FileInfo(path))
                              .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
                              .Select(fileInfo => fileInfo.FullName)
                              .ToArray();

                Files = Directory.GetFiles(UploadsPath)
                            .Select(path => new FileInfo(path))
                            .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
                            .Select(fileInfo => fileInfo.FullName)
                            .ToArray();


                isDataLoaded = true;

            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message.ToString();
                isDataLoaded = false;
                RedirectToPage();
            }
        }

        public static bool IsImageFile(string extension)
        {
            return imageExtensions.Contains(extension.ToLower());
        }

        public static string GetImageInfo(string filePath)
        {
            using (MagickImage image = new MagickImage(filePath))
            {
                return $"{image.Width} x {image.Height}";
            }
        }

        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            int suffixIndex = 0;

            double fileSize = bytes;
            while (fileSize >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                fileSize /= 1024;
                suffixIndex++;
            }

            return $"{fileSize:N2} {suffixes[suffixIndex]}";
        }


        public async Task<IActionResult> OnGetDownloadFile(string filePath)
        {


            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = GetContentType(filePath);
            var fileName = System.IO.Path.GetFileName(filePath);

            
           
            return File(fileBytes, contentType, fileName);
        }

        public IActionResult OnGetDownloadZip(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))
            {
                return NotFound();
            }

            string zipFileName = $"{Path.GetFileName(dirPath)}.zip";
            string zipFilePath = Path.Combine(Path.GetTempPath(), zipFileName);

            try
            {
                ZipFile.CreateFromDirectory(dirPath, zipFilePath);

                byte[] fileBytes = System.IO.File.ReadAllBytes(zipFilePath);
                string contentType = "application/zip";

                return File(fileBytes, contentType, zipFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
            finally
            {
                System.IO.File.Delete(zipFilePath);
            }
        }

        private string GetContentType(string filePath)
        {

            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {

                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".tiff":
                case ".tif":
                    return "image/tiff";
                case ".ico":
                    return "image/x-icon";
                default:
                    return "application/octet-stream";
            }
        }






    }
}
