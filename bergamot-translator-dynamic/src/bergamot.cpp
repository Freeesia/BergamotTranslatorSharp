#include "bergamot.h"
#include "3rd_party/marian-dev/src/3rd_party/spdlog/spdlog.h"
#include "translator/response.h"
#include "translator/response_options.h"
#include "translator/service.h"
#include "translator/translation_model.h"
#ifdef _WIN32
#include <combaseapi.h>
#else
#include <gperftools/tcmalloc.h>
#endif

using marian::bergamot::BlockingService;
using marian::bergamot::parseOptionsFromFilePath;
using marian::bergamot::Response;
using marian::bergamot::ResponseOptions;
using marian::bergamot::TranslationModel;

// トランスレーターの状態を保持する構造体
struct BergamotTranslatorState
{
    // BlockingServiceとTranslationModelのインスタンスを保持
    std::unique_ptr<BlockingService> service;
    std::vector<std::shared_ptr<TranslationModel>> models;

    BergamotTranslatorState() {}
    ~BergamotTranslatorState() {}
};

extern "C"
{
    void *translator_initialize(const char **configPaths, int numPaths)
    {
        if (!configPaths || numPaths <= 0 || numPaths > 2)
            return nullptr;

        // BlockingServiceの設定
        BlockingService::Config serviceConfig;

        // トランスレーターの状態オブジェクト作成
        auto state = new BergamotTranslatorState();

        // BlockingServiceのインスタンス作成
        state->service = std::make_unique<BlockingService>(serviceConfig);
        // ログが重複するとエラーになり複数インスタンス作れないので、全てのログをドロップ
        spdlog::drop_all();

        // 各設定ファイルからモデルを作成
        for (int i = 0; i < numPaths; i++)
        {
            if (configPaths[i])
            {
                auto options = parseOptionsFromFilePath(configPaths[i]);
                state->models.push_back(std::make_shared<TranslationModel>(options));
            }
        }

        // モデルがひとつもロードできなかった場合は失敗
        if (state->models.empty())
        {
            delete state;
            return nullptr;
        }

        return static_cast<void *>(state);
    }

    char *translator_translate(void *translator, const char *text)
    {
        if (!translator || !text)
        {
            return nullptr;
        }

        auto state = static_cast<BergamotTranslatorState *>(translator);

        // 翻訳オプションの設定
        ResponseOptions responseOptions;

        // 翻訳実行（最初のモデルを使用）
        std::vector<std::string> sources = {text};
        std::vector<ResponseOptions> options = {responseOptions};

        std::vector<Response> responses;
        if (state->models.size() == 1)
        {
            responses = state->service->translateMultiple(state->models[0], std::move(sources), options);
        }
        else if (state->models.size() == 2)
        {
            responses = state->service->pivotMultiple(state->models[0], state->models[1], std::move(sources), options);
        }
        else
        {
            return nullptr;
        }

        // 翻訳結果を取得
        if (!responses.empty())
        {
            auto translated = responses[0].target.text;
            size_t len = translated.size() + 1;
#ifdef _WIN32
            char *result = (char *)CoTaskMemAlloc(len);
#else
            // tcmallocを直接使用してメモリ確保
            char *result = (char *)malloc(len);
#endif
            memcpy(result, translated.c_str(), len);
            return result;
        }

        return nullptr;
    }

    void translator_free(void *translator)
    {
        if (translator)
        {
            auto state = static_cast<BergamotTranslatorState *>(translator);
            delete state;
        }
    }

} // extern "C"
