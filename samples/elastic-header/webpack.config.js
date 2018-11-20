const path = require("path");
const VueLoaderPlugin = require('vue-loader/lib/plugin')

function resolve(x) {
  return path.join(__dirname, x);
}

module.exports = {
  entry: resolve("./src/ElasticHeader.fsproj"),
  output: {
    path: resolve("public"),
    filename: "bundle.js"
  },
  devServer: {
    contentBase: resolve("public"),
    port: 8080,
  },
  mode: "development",
  devtool: "eval-source-map",
  module: {
    rules: [
      {
        test: /\.fs(x|proj)?$/,
        use: "fable-loader"
      },
      {
        test: /\.html$/,
        use: "raw-loader"
      },
      {
        test: /\.vue$/,
        loader: 'vue-loader'
      },
      {
        test: /\.css$/,
        use: [
          'vue-style-loader',
          'css-loader'
        ]
      }
    ]
  },
  plugins: [
    new VueLoaderPlugin()
  ]
};
