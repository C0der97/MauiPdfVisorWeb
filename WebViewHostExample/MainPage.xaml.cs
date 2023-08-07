using Kotlin;
using WebViewHostExample.Controls;
using WebViewHostExample.ViewModels;

namespace WebViewHostExample;

public partial class MainPage : ContentPage
{
	int count = 0;
    MainPageViewModel vm;

    public bool Navigating { get; set; } = false;

	public MainPage()
	{
		InitializeComponent();

        vm = new MainPageViewModel();
        MyWebView.BindingContext = vm;
        MyWebView.JavaScriptAction += MyWebView_JavaScriptAction;
        //MyWebView.Source = "https://prucorporativo.compensar.com/";
        MyWebView.NavigateEvent += MyWebView_NavigateEvent;
    }

    private async Task<bool> GetPDf()
    {
        using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync("file.pdf");
        using StreamReader reader = new StreamReader(fileStream);

        byte[] data;
        using (MemoryStream ms = new MemoryStream())
        {
            fileStream.CopyTo(ms);
            data = ms.ToArray();
        }

        string bs = Convert.ToBase64String(data);

        await MyWebView.EvaluateJavaScriptAsync(new EvaluateJavaScriptAsyncRequest("SetPdf('"+ bs + "')"));

        return true;
    }


    private void MyWebView_NavigateEvent(bool flag)
    {
        Indicador.IsRunning = flag;
        Indicador.IsVisible = flag;
        Info.IsVisible = flag;
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        vm.Source = new HtmlWebViewSource() { Html = htmlSource };
    }

    private void MyWebView_JavaScriptAction(object sender, Controls.JavaScriptActionEventArgs e)
    {
		Dispatcher.Dispatch(() =>
		{
            //ChangeLabel.Text = "The Web Button Was Clicked! Count: " + e.Payload;
        });
    }

    string htmlSource = @"
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/pdf.js/2.6.347/pdf.min.js' integrity='sha512-Z8CqofpIcnJN80feS2uccz+pXWgZzeKxDsDNMD/dJ6997/LSRY+W4NmEt9acwR+Gt9OHN0kkI1CTianCwoqcjQ==' crossorigin='anonymous' referrerpolicy='no-referrer'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/pdf.js/2.6.347/pdf.worker.min.js' integrity='sha512-lHibs5XrZL9hXP3Dhr/d2xJgPy91f2mhVAasrSbMkbmoTSm2Kz8DuSWszBLUg31v+BM6tSiHSqT72xwjaNvl0g==' crossorigin='anonymous' referrerpolicy='no-referrer'></script>


    <style>
        #the-canvas {
            Width: 100%;
            border: 1px solid black;
            direction: ltr;
        }

        .bigBtn {
            background-color: Orange;
            color: white;
            border: 0px transparent;
            width: 30%;
            height: 10%;
            font-size: 1rem;
            border-radius: 1%;
        }

        .texto {
            font-size: 1rem;
        }

        .centrado {
            margin: 0 auto;
            text-align: center;
            display: flex;
            justify-content: center;
        }

        .mb {
            margin-top: 10%;
        }

        .ml {
            margin-left: 5px;
        }
    </style>
</head>
<body>
<canvas class='centrado' id='the-canvas'></canvas>
<div class='centrado mb'>
  <button class='bigBtn' id='prev'><</button>
  <button class='bigBtn ml' id='next'>></button>
  &nbsp; &nbsp;
  <span class='texto'>Página: <span id='page_num'></span> / <span id='page_count'></span></span>

 <button class='bigBtn' onclick='PlusScale()' id='plusZoom'>+</button>
 <button class='bigBtn' onclick='LessScale()' id='minusZoom'>-</button>
</div>
<script>

// If absolute URL from the remote server is provided, configure the CORS
// header on that server.
var url = 'https://raw.githubusercontent.com/mozilla/pdf.js/ba2edeae/web/compressed.tracemonkey-pldi-09.pdf';

// Loaded via <script> tag, create shortcut to access PDF.js exports.
var pdfjsLib = window['pdfjs-dist/build/pdf'];

// The workerSrc property shall be specified.
    pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/2.6.347/pdf.worker.min.js';

var pdfDoc = null,
    pageNum = 1,
    pageRendering = false,
    pageNumPending = null,
    scale = 0.8,
    canvas = document.getElementById('the-canvas'),
    ctx = canvas.getContext('2d');

/**
 * Get page info from document, resize canvas accordingly, and render page.
 * @param num Page number.
 */
function renderPage(num) {
  pageRendering = true;
  // Using promise to fetch the page
  pdfDoc.getPage(num).then(function(page) {
    var viewport = page.getViewport({scale: scale});
    canvas.height = viewport.height;
    canvas.width = viewport.width;

    // Render PDF page into canvas context
    var renderContext = {
      canvasContext: ctx,
      viewport: viewport
    };
    var renderTask = page.render(renderContext);

    // Wait for rendering to finish
    renderTask.promise.then(function() {
      pageRendering = false;
      if (pageNumPending !== null) {
        // New page rendering is pending
        renderPage(pageNumPending);
        pageNumPending = null;
      }
    });
  });

  // Update page counters
  document.getElementById('page_num').textContent = num;
}

/**
 * If another page rendering in progress, waits until the rendering is
 * finised. Otherwise, executes rendering immediately.
 */
function queueRenderPage(num) {
  if (pageRendering) {
    pageNumPending = num;
  } else {
    renderPage(num);
  }
}

/**
 * Displays previous page.
 */
function onPrevPage() {
  if (pageNum <= 1) {
    return;
  }
  pageNum--;
  queueRenderPage(pageNum);
}
document.getElementById('prev').addEventListener('click', onPrevPage);

/**
 * Displays next page.
 */
function onNextPage() {
  if (pageNum >= pdfDoc.numPages) {
    return;
  }
  pageNum++;
  queueRenderPage(pageNum);
}
document.getElementById('next').addEventListener('click', onNextPage);

/**
 * Asynchronously downloads PDF.
 */



function SetPdf(pdfData){
  var pdfInfo = atob(pdfData);

    var loadingTask = pdfjsLib.getDocument({ data: pdfInfo });

    loadingTask.promise.then(function (pdfDoc_) {
        pdfDoc = pdfDoc_;
        document.getElementById('page_count').textContent = pdfDoc.numPages;

        // Initial/first page rendering
        renderPage(pageNum);
    });

  document.getElementById('page_count').textContent = pdfDoc.numPages;

  // Initial/first page rendering
  renderPage(pageNum);

}

function PlusScale(){
    
    scale += 0.1;
  renderPage(1);
}

function LessScale(){
 scale -= 0.1;
  renderPage(1);
}

</script>
</body>

</html>
";

    private async void EvalButton_ClickedAsync(object sender, EventArgs e)
    {
       await GetPDf();
    }
}

