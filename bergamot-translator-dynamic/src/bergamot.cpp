#include "bergamot.h"
#include "3rd_party/marian-dev/src/3rd_party/spdlog/spdlog.h"
#include "translator/response.h"
#include "translator/response_options.h"
#include "translator/service.h"
#include "translator/translation_model.h"
#include <cstdlib>
#include <cstring>
#include <limits>
#include <string>
#include <utility>
#include <vector>
#ifdef _WIN32
#include <combaseapi.h>
#endif

using marian::bergamot::BlockingService;
using marian::bergamot::parseOptionsFromFilePath;
using marian::bergamot::Response;
using marian::bergamot::ResponseOptions;
using marian::bergamot::TranslationModel;

namespace
{
    void *allocateTranslationMemory(size_t size)
    {
#ifdef _WIN32
        return CoTaskMemAlloc(size);
#else
        return std::malloc(size);
#endif
    }

    void freeTranslationMemory(void *memory)
    {
#ifdef _WIN32
        CoTaskMemFree(memory);
#else
        std::free(memory);
#endif
    }

    char *copyTranslation(const std::string &text)
    {
        auto *result = static_cast<char *>(allocateTranslationMemory(text.size() + 1));
        if (!result)
            return nullptr;

        std::memcpy(result, text.c_str(), text.size() + 1);
        return result;
    }
}

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
                // Marian must collect soft alignments while decoding so that a later
                // HTML request can transfer source tags to the translated tokens.
                options->set("alignment", "soft");
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

    char *translator_translate(void *translator, const char *text, bool html)
    {
        if (!translator || !text)
        {
            return nullptr;
        }

        auto state = static_cast<BergamotTranslatorState *>(translator);

        // 翻訳オプションの設定
        ResponseOptions responseOptions;
        responseOptions.HTML = html;

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
            return copyTranslation(responses[0].target.text);
        }

        return nullptr;
    }

    char **translator_translate_multiple(void *translator, const char **texts, size_t count)
    {
        if (!translator || !texts || count == 0 || count > std::numeric_limits<size_t>::max() / sizeof(char *))
            return nullptr;

        try
        {
            auto state = static_cast<BergamotTranslatorState *>(translator);
            if (state->models.empty() || state->models.size() > 2)
                return nullptr;

            std::vector<std::string> sources;
            sources.reserve(count);
            for (size_t i = 0; i < count; ++i)
            {
                if (!texts[i])
                    return nullptr;
                sources.emplace_back(texts[i]);
            }

            // The batch API translates plain text; leave HTML handling disabled for each input.
            std::vector<ResponseOptions> options(count);
            std::vector<Response> responses;
            if (state->models.size() == 1)
            {
                responses = state->service->translateMultiple(state->models[0], std::move(sources), options);
            }
            else
            {
                responses = state->service->pivotMultiple(state->models[0], state->models[1], std::move(sources), options);
            }

            if (responses.size() != count)
                return nullptr;

            auto **translations = static_cast<char **>(allocateTranslationMemory(sizeof(char *) * count));
            if (!translations)
                return nullptr;

            for (size_t i = 0; i < count; ++i)
                translations[i] = nullptr;

            for (size_t i = 0; i < count; ++i)
            {
                translations[i] = copyTranslation(responses[i].target.text);
                if (!translations[i])
                {
                    translator_free_translations(translations, count);
                    return nullptr;
                }
            }

            return translations;
        }
        catch (...)
        {
            // Never allow a C++ exception to cross the C ABI boundary.
            return nullptr;
        }
    }

    void translator_free_translations(char **translations, size_t count)
    {
        if (!translations)
            return;

        for (size_t i = 0; i < count; ++i)
            freeTranslationMemory(translations[i]);
        freeTranslationMemory(translations);
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
