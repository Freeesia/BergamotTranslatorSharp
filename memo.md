# メモ

## Intel oneAPI
### ダウンロード
* https://www.intel.com/content/www/us/en/developer/tools/oneapi/onemkl-download.html?operatingsystem=windows&windows-install=offline
  * こっちはMLKだけ
  * GUIのインストーラーしかない？
* https://www.intel.com/content/www/us/en/developer/tools/oneapi/base-toolkit-download.html
  * こっちは全部
  * CLIのインストーラーがあるっぽい

## PCRE2

### インストール

`vcpkg install pcre2`でインストールしたけど、パスの通し方が分からない…
`-DSSPLIT_USE_INTERNAL_PCRE2=ON`を付けてcmakeすると、👆は使わないけど通った

## ビルド

```bat
cd bergamot-translator\build-native
cmake ../ -G "NMake Makefiles" -DCMAKE_BUILD_TYPE=Release -DSSPLIT_USE_INTERNAL_PCRE2=ON
REM cmake ../ -G "Visual Studio 17 2022" -DCMAKE_BUILD_TYPE=Release -DSSPLIT_USE_INTERNAL_PCRE2=ON
nmake
```

ビルドが通らない...
ファイルのコードページが932として認識されて(実際はUTF-8)、処理出来ない文字が存在するっぽい。
ファイルをUTF-8(BOM)に変換して再度ビルドすると通りそう。

`bergamot-translator\3rd_party\marian-dev\CMakeLists.txt`の`CMAKE_CXX_FLAGS`に`/utf-8`を足すと落ちてたところが通った。

👆だと結局PCRE2が無理なので、vckg通す
```bat
mkdir build-win
cmake -DCMAKE_TOOLCHAIN_FILE="%VCPKG_ROOT%/scripts/buildsystems/vcpkg.cmake" -DCMAKE_BUILD_TYPE=Release -DUSE_STATIC_LIBS=ON -DVCPKG_TARGET_TRIPLET="x64-windows-static" ..
cmake --build . --config Release --target bergamot_translator_dynamic
cmake --install . --prefix "../libs" --component bergamot_translator_dynamic
```

これでも`-- Not Found TCMalloc: TCMALLOC_LIB-NOTFOUND`がでる


`bergamot-translator\.github\workflows\windows.yml`にビルド方法あるやん。

#### Linux

```bash
mkdir build-linux
cmake -DCMAKE_BUILD_TYPE=Release -DUSE_STATIC_LIBS=ON ..
cmake --build . --config Release --target bergamot_translator_dynamic -j12
cmake --install . --prefix "../libs" --component bergamot_translator_dynamic
```


## 実行

```bat
echo "Hello World!" | build-native\app\Debug\bergamot.exe --model-config-paths models\enja\config.yml
```

## やること

* [x] Windows版のビルド通す
* [x] Windows版の実行確認
* [x] ネイティブ動的ライブラリの作成
* [x] ネイティブ動的ライブラリを参照した.NETライブラリの作成
* [x] .NETライブラリを参照したサンプルアプリの作成
* [x] NuGetパッケージの作成
* [x] ドキュメントの作成
* [ ] テストの作成
* [x] CI/CDの設定
* [x] リリース
