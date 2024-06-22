using CsvHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Data.Common;
using QuestPDF.ExampleInvoice;
using System.IO;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace WinFormsApp4
{
    public partial class ManageBook : Form
    {
        private ProductsContext? dbContext;

        public ManageBook()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.dbContext = new ProductsContext();

            // Uncomment the line below to start fresh with a new database.
            //this.dbContext.Database.EnsureDeleted();
            this.dbContext.Database.EnsureCreated();

            this.dbContext.Categories.Load();

            this.categoryBindingSource.DataSource = dbContext.Categories.Local.ToBindingList();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            this.dbContext?.Dispose();
            this.dbContext = null;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            //this.dbContext.Categories.Load();
            if (this.dbContext != null)
            {
                var category = (Category)this.dataGridView1.CurrentRow.DataBoundItem;

                if (category != null)
                {
                    this.dbContext.Entry(category).Collection(e => e.Products).Load();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.dbContext!.SaveChanges();

            this.dataGridView1.Refresh();
            this.dataGridView2.Refresh();
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            importFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            importFileDialog.Title = "Select a CSV file";

            if (importFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = importFileDialog.FileName;
                ImportCsvToSqlite(filePath);
            }
        }

        //Make sure CSVHelper is installed.
        private void ImportCsvToSqlite(string csvFilePath)
        {
            try
            {
                if (this.dbContext != null)
                {
                    using (var reader = new StreamReader(csvFilePath))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<Product>().ToList();

                        foreach (var record in records)
                        {
                            // Check if the category exists in the database
                            var existingCategory = this.dbContext.Categories
                                .FirstOrDefault(c => c.CategoryId == record.CategoryId);

                            if (existingCategory != null)
                            {
                                // Attach the existing category to the product
                                record.Category = existingCategory;
                            }
                            else
                            {
                                // If category does not exist, create a new one
                                var newCategory = new Category
                                {
                                    CategoryId = record.CategoryId,
                                    Name = record.Category.Name // Assuming CSV contains category name
                                };
                                this.dbContext.Categories.Add(newCategory);
                                record.Category = newCategory;
                            }

                            // Add or update the product
                            var existingProduct = this.dbContext.Products
                                .FirstOrDefault(p => p.ProductId == record.ProductId);

                            if (existingProduct != null)
                            {
                                existingProduct.Name = record.Name;
                                existingProduct.CategoryId = record.CategoryId;
                                existingProduct.Category = record.Category;
                            }
                            else
                            {
                                this.dbContext.Products.Add(record);
                            }
                        }
                        this.dbContext.SaveChanges();
                    }
                    MessageBox.Show("CSV data imported successfully!");
                }
            }
            catch (Exception err)
            {
                Console.Write(err.ToString());
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            //if (printDialog.ShowDialog() == DialogResult.OK)
            //{
            //    printDocument.Print();
            //}
            var model = InvoiceDocumentDataSource.GetInvoiceDetails();
            var document = new InvoiceDocument(model);
            document.GeneratePdfAndShow();
        }
        private bool isTitlePagePrinted = false;
        private int currentProductIndex = 0;
        private void printDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (dbContext == null)
            {
                return;
            }

            Font titleFont = new Font("Arial", 16, FontStyle.Bold);
            Font headerFont = new Font("Arial", 10, FontStyle.Bold);
            Font font = new Font("Arial", 10);
            float lineHeight = font.GetHeight(e.Graphics);
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;
            float tableWidth = e.MarginBounds.Width;
            float[] columnWidths = { tableWidth * 0.1f, tableWidth * 0.4f, tableWidth * 0.5f }; // Adjust as needed

            // Print the title page
            if (!isTitlePagePrinted)
            {
                string title = "Products and Categories Report";
                SizeF titleSize = e.Graphics.MeasureString(title, titleFont);
                e.Graphics.DrawString(title, titleFont, Brushes.Black, (e.PageBounds.Width - titleSize.Width) / 2, e.MarginBounds.Top + 100);

                isTitlePagePrinted = true;
                e.HasMorePages = true; // Indicate that there are more pages to print
                return;
            }

            // Print the header
            string[] headers = { "Product ID", "Product Name", "Category Name" };
            for (int i = 0; i < headers.Length; i++)
            {
                e.Graphics.DrawRectangle(Pens.Black, x, y, columnWidths[i], lineHeight);
                e.Graphics.DrawString(headers[i], headerFont, Brushes.Black, x, y);
                x += columnWidths[i];
            }

            x = e.MarginBounds.Left;
            y += lineHeight;

            // Print the data rows
            for (; currentProductIndex < dbContext.Products.Include(p => p.Category).Count(); currentProductIndex++)
            {
                var product = dbContext.Products.Include(p => p.Category).ToList()[currentProductIndex];

                e.Graphics.DrawRectangle(Pens.Black, x, y, columnWidths[0], lineHeight);
                e.Graphics.DrawString(product.ProductId.ToString(), font, Brushes.Black, x, y);

                x += columnWidths[0];

                e.Graphics.DrawRectangle(Pens.Black, x, y, columnWidths[1], lineHeight);
                e.Graphics.DrawString(product.Name, font, Brushes.Black, x, y);

                x += columnWidths[1];

                e.Graphics.DrawRectangle(Pens.Black, x, y, columnWidths[2], lineHeight);
                e.Graphics.DrawString(product.Category.Name, font, Brushes.Black, x, y);

                x = e.MarginBounds.Left;
                y += lineHeight;

                // Check if adding another row exceeds the page bounds
                if (y + lineHeight > e.MarginBounds.Bottom)
                {
                    currentProductIndex++; // Move to the next product for the next page
                    e.HasMorePages = true;
                    return;
                }
            }

            // Indicate that no more pages are needed
            e.HasMorePages = false;
            isTitlePagePrinted = false; // Reset for next print job
            currentProductIndex = 0; // Reset current index for next print job
        }
        private void btnPrintPreview_Click(object sender, EventArgs e)
        {
            PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog
            {
                Document = printDocument,
                Width = 800,
                Height = 600
            };

            printPreviewDialog.ShowDialog();
        }

        private void txtSearchCategory_TextChanged(object sender, EventArgs e)
        {
            var categoryList = this.dbContext.Categories.Where(x => x.Name.ToLower().Contains(txtSearchCategory.Text));
            categoryList.Load();
            this.categoryBindingSource.DataSource = categoryList.ToList();
            this.dataGridView1.Refresh();
        }

        private void ManageBook_Load(object sender, EventArgs e)
        {

        }
    }
}
