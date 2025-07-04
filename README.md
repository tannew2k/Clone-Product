# 🛍️ Clone Product

**Clone Product** is a powerful tool that allows sellers to extract product data from various e-commerce platforms like **Shopify**, **ShopBase**, **WooCommerce**, **Sellless**, and **OnePage**, and push it directly to their **WooCommerce** store. This is especially useful for dropshippers, store managers, or marketers looking to streamline product imports and reduce manual entry.

---

## 📌 Features

✅ Clone product details (title, images, price, description, etc.)  
✅ Supports multiple platforms:  
&nbsp;&nbsp;&nbsp;&nbsp;• Shopify  
&nbsp;&nbsp;&nbsp;&nbsp;• ShopBase  
&nbsp;&nbsp;&nbsp;&nbsp;• WooCommerce  
&nbsp;&nbsp;&nbsp;&nbsp;• Sellless  
&nbsp;&nbsp;&nbsp;&nbsp;• OnePage  
✅ Push products directly to your WooCommerce store  
✅ Headless or browser-based automation using Selenium  
✅ Easy setup and configuration

---

## 🧰 Technologies Used

- **.NET (C#)** – Application Framework  
- **Selenium WebDriver** – Web automation and crawling

---

## 📦 Prerequisites

Make sure you have the following installed:

- [.NET SDK](https://dotnet.microsoft.com/en-us/download)
- [Google Chrome](https://www.google.com/chrome/) (for Selenium)
- [ChromeDriver](https://chromedriver.chromium.org/downloads) (Ensure it matches your Chrome version)
- WooCommerce store with REST API credentials (Consumer Key & Secret)

---

## 🚀 Getting Started

1. Clone the repository:

```bash
git clone https://github.com/tannew2k/Clone-Product.git
cd Clone-Product
```

2. Install dependencies:

```bash
dotnet restore
```

3. Configure the application:

- Create a `config.json` file in the root directory.
- Add your WooCommerce store URL, Consumer Key, and Consumer Secret.
- Add your ChromeDriver path Asiup-Clone-Product\bin\Debug\net\chromedriver.exe

4. Run the application:

```bash
dotnet run
```

5. Input the product URL and select the platform.

## Work

1. Input the product URL, load the url using Selenium WebDriver
2. Choose platform (Shopify, ShopBase, WooCommerce, Sellless, OnePage) via website cdn
3. This tool will extract product details and push to your WooCommerce store, via product info api, or use xpath to get the information.


