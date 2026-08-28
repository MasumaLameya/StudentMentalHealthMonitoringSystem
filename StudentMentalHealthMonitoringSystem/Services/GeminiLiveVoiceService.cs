using System.Text;
using System.Text.Json;
using StudentMentalHealthMonitoringSystem.Models;

namespace StudentMentalHealthMonitoringSystem.Services
{
    public class GeminiLiveVoiceService
    {
        // =========================================================
        // GEMINI AUTH TOKEN API
        // =========================================================

        private const string AuthTokenUrl =
            "https://generativelanguage.googleapis.com/" +
            "v1beta/auth_tokens";


        // =========================================================
        // DEPENDENCIES
        // =========================================================

        private readonly IConfiguration
            _configuration;


        private readonly HttpClient
            _httpClient;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public GeminiLiveVoiceService(
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _configuration =
                configuration;


            _httpClient =
                httpClient;
        }


        // =========================================================
        // CREATE EPHEMERAL / TEMPORARY LIVE TOKEN
        // =========================================================
        //
        // Permanent Gemini API key:
        //
        // appsettings.json
        //       ↓
        // ASP.NET Backend
        //       ↓
        // Gemini Auth Token API
        //       ↓
        // Short-lived token
        //       ↓
        // Browser
        //       ↓
        // Gemini Live API
        //
        // The permanent API key is never returned
        // to the browser.
        //
        // =========================================================

        public async Task<string>
            CreateEphemeralTokenAsync(
                string requestedModel,
                CancellationToken cancellationToken =
                    default)
        {
            // =====================================================
            // GET PERMANENT SERVER-SIDE API KEY
            // =====================================================

            var apiKey =
                GetApiKey();


            // =====================================================
            // GET CONFIGURED LIVE MODEL
            // =====================================================

            var configuredLiveModel =
                GetLiveModelName();


            // =====================================================
            // VALIDATE REQUESTED MODEL
            // =====================================================
            //
            // VoiceBotController sends the model stored in the
            // current VoiceBotSession.
            //
            // It must match appsettings.json.
            //
            // =====================================================

            requestedModel =
                requestedModel?.Trim()
                ?? string.Empty;


            if (string.IsNullOrWhiteSpace(
                requestedModel))
            {
                throw new InvalidOperationException(
                    "Gemini Live Voice model is missing."
                );
            }


            if (!string.Equals(
                requestedModel,
                configuredLiveModel,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The Voice Bot session model does not match " +
                    "the configured Gemini Live Voice model."
                );
            }


            // =====================================================
            // TOKEN EXPIRATION
            // =====================================================
            //
            // Token remains usable for the current Live session.
            //
            // A new Live session must start within one minute.
            //
            // =====================================================

            var now =
                DateTimeOffset.UtcNow;


            var expireTime =
                FormatUtcTimestamp(
                    now.AddMinutes(
                        30
                    )
                );


            var newSessionExpireTime =
                FormatUtcTimestamp(
                    now.AddMinutes(
                        1
                    )
                );


            // =====================================================
            // EPHEMERAL TOKEN REQUEST
            // =====================================================
            //
            // IMPORTANT:
            //
            // liveConnectConstraints is intentionally NOT included.
            //
            // The Gemini auth_tokens endpoint currently used by
            // this project returned HTTP 400 when that field was
            // included.
            //
            // The model, AUDIO response modality and transcription
            // settings are configured later in Index.cshtml when
            // the browser opens the Gemini Live connection.
            //
            // =====================================================

            var payload =
                new
                {
                    uses =
                        1,


                    expireTime =
                        expireTime,


                    newSessionExpireTime =
                        newSessionExpireTime
                };


            // =====================================================
            // CREATE HTTP REQUEST
            // =====================================================

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    AuthTokenUrl
                );


            // =====================================================
            // AUTHENTICATE WITH PERMANENT API KEY
            // =====================================================

            request.Headers.Add(
                "x-goog-api-key",
                apiKey
            );


            request.Content =
                new StringContent(
                    JsonSerializer.Serialize(
                        payload
                    ),
                    Encoding.UTF8,
                    "application/json"
                );


            // =====================================================
            // SEND TOKEN REQUEST
            // =====================================================

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken
                );


            var responseText =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken
                    );


            // =====================================================
            // TOKEN REQUEST FAILED
            // =====================================================

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "Gemini Live token creation failed. " +
                    $"HTTP Status: {(int)response.StatusCode}. " +
                    $"Response: {responseText}"
                );
            }


            // =====================================================
            // PARSE RESPONSE
            // =====================================================

            using var document =
                JsonDocument.Parse(
                    responseText
                );


            var root =
                document.RootElement;


            // =====================================================
            // TOKEN IS RETURNED IN:
            //
            // name
            //
            // =====================================================

            if (!root.TryGetProperty(
                "name",
                out var nameElement))
            {
                throw new InvalidOperationException(
                    "Gemini did not return an ephemeral token."
                );
            }


            var token =
                nameElement.GetString();


            if (string.IsNullOrWhiteSpace(
                token))
            {
                throw new InvalidOperationException(
                    "Gemini returned an empty ephemeral token."
                );
            }


            return token.Trim();
        }


        // =========================================================
        // ANALYZE CURRENT CONVERSATION
        // =========================================================
        //
        // Called after meaningful completed conversation turns.
        //
        // Uses all transcripts currently stored for the session.
        //
        // Returns exactly one project-level status:
        //
        // Normal
        // Moderate
        // Severe
        // Extremely Severe
        //
        // + concise rolling summary
        //
        // =========================================================

        public async Task<VoiceBotAnalysisResult>
            AnalyzeCurrentConversationAsync(
                IEnumerable<VoiceBotTranscript> transcripts,
                CancellationToken cancellationToken =
                    default)
        {
            // =====================================================
            // VALIDATE TRANSCRIPT
            // =====================================================

            if (transcripts == null)
            {
                return new VoiceBotAnalysisResult
                {
                    Status =
                        "Normal",

                    Summary =
                        "No conversation information is available yet."
                };
            }


            var orderedTranscripts =
                transcripts
                    .OrderBy(t =>
                        t.CreatedAt
                    )
                    .ToList();


            if (!orderedTranscripts.Any())
            {
                return new VoiceBotAnalysisResult
                {
                    Status =
                        "Normal",

                    Summary =
                        "The live conversation has just started."
                };
            }


            // =====================================================
            // GET LATEST STUDENT TURN
            // =====================================================
            //
            // The latest Student turn is analyzed separately so a
            // new higher-concern statement is not diluted by older,
            // lower-concern conversation context.
            //
            // =====================================================

            var latestStudentTranscript =
                orderedTranscripts

                    .Where(t =>
                        t.Speaker ==
                            "Student"
                    )

                    .OrderByDescending(t =>
                        t.CreatedAt
                    )

                    .FirstOrDefault();


            var latestStudentTurn =
                latestStudentTranscript?
                    .TranscriptText?
                    .Trim()
                ?? string.Empty;


            // =====================================================
            // BUILD COMPLETE CURRENT CONVERSATION
            // =====================================================

            var conversationBuilder =
                new StringBuilder();


            foreach (var transcript
                in orderedTranscripts)
            {
                conversationBuilder
                    .Append(
                        transcript.Speaker
                    )
                    .Append(": ")
                    .AppendLine(
                        transcript.TranscriptText
                    );
            }


            var conversation =
                conversationBuilder
                    .ToString();


            // =====================================================
            // STATUS ANALYSIS INSTRUCTION
            // =====================================================
            //
            // IMPORTANT:
            //
            // Gemini returns TWO monitoring classifications in ONE
            // API request:
            //
            // 1. latestTurnStatus
            // 2. overallStatus
            //
            // The application then uses the higher-concern result
            // as the current status.
            //
            // This keeps analysis fast while making new important
            // Student turns immediately visible in monitoring.
            //
            // =====================================================

            var prompt = $@"
You are performing a monitoring classification for a
university Student Mental Health Monitoring System.

This is NOT a medical diagnosis.

You must evaluate TWO things independently:

1. LATEST STUDENT TURN
2. COMPLETE CONVERSATION CONTEXT

Use exactly these project-level categories:

Normal
Moderate
Severe
Extremely Severe


GENERAL INTERPRETATION

Normal:
No significant current distress or functional difficulty
is evident from the available information.

Moderate:
Noticeable emotional distress or difficulty is present,
but routine support and monitoring are appropriate.

Severe:
Substantial distress, significant difficulty in daily
functioning, or a serious current safety concern is
evident and prompt professional review is appropriate.

Extremely Severe:
The available information indicates a very high or
urgent level of concern requiring immediate human
professional attention.


IMPORTANT CLASSIFICATION RULES

- Assess the latest Student turn independently first.
- Then assess the complete conversation independently.
- Give strong importance to the most recent Student turn
  when it clearly describes the student's CURRENT state.
- Do not allow older normal or reassuring conversation to
  dilute a clearly more concerning current Student turn.
- Do not escalate severity from vague wording alone.
- Consider context, meaning, current functioning, and
  whether the concern appears current.
- Do not diagnose any medical or psychiatric condition.
- Do not exaggerate severity.
- Do not minimize significant concerns.
- The summary must be concise and professional.
- The summary must describe the current overall condition
  without quoting sensitive conversation details.

TRANSCRIPT CLEANUP

- correctedLatestStudentText must always be written in
  natural English using the Latin alphabet only.
- Review only the LATEST STUDENT TURN for speech-to-text
  recognition errors.
- If English speech was phonetically written in Devanagari,
  Bengali, or another non-Latin script, convert it back to
  the intended natural English wording.
- If the Student actually spoke another language, provide a
  faithful English rendering for correctedLatestStudentText
  so the stored/displayed transcript remains English-only.
- Correct a word or spacing error only when the intended
  wording is clear from the sentence and conversation context.
- Preserve the Student's original meaning as closely as
  possible.
- Never add, remove, strengthen, weaken, or reverse negation,
  intent, safety meaning, or severity-related meaning.
- If wording is uncertain, choose the closest faithful English
  rendering without inventing new meaning.
- The summary must also be written in English only.
- Return only the requested structured result.


LATEST STUDENT TURN:

{latestStudentTurn}


COMPLETE CONVERSATION:

{conversation}
";


            // =====================================================
            // STRUCTURED JSON REQUEST
            // =====================================================

            var payload =
                new
                {
                    contents =
                        new[]
                        {
                            new
                            {
                                role =
                                    "user",

                                parts =
                                    new[]
                                    {
                                        new
                                        {
                                            text =
                                                prompt
                                        }
                                    }
                            }
                        },


                    generationConfig =
                        new
                        {
                            // =====================================
                            // GENERATECONTENT STRUCTURED OUTPUT
                            // =====================================
                            //
                            // Use the generateContent-compatible
                            // fields here. This avoids the
                            // TextResponseFormat enum mismatch that
                            // caused HTTP 400 with:
                            //
                            // responseFormat.text.mimeType =
                            // "application/json"
                            //
                            // =====================================

                            responseMimeType =
                                "application/json",


                            responseSchema =
                                new
                                {
                                    type =
                                        "object",

                                    properties =
                                        new
                                        {
                                            latestTurnStatus =
                                                new
                                                {
                                                    type =
                                                        "string",

                                                    @enum =
                                                        new[]
                                                        {
                                                            "Normal",
                                                            "Moderate",
                                                            "Severe",
                                                            "Extremely Severe"
                                                        }
                                                },


                                            overallStatus =
                                                new
                                                {
                                                    type =
                                                        "string",

                                                    @enum =
                                                        new[]
                                                        {
                                                            "Normal",
                                                            "Moderate",
                                                            "Severe",
                                                            "Extremely Severe"
                                                        }
                                                },


                                            summary =
                                                new
                                                {
                                                    type =
                                                        "string"
                                                },


                                            correctedLatestStudentText =
                                                new
                                                {
                                                    type =
                                                        "string"
                                                }
                                        },


                                    required =
                                        new[]
                                        {
                                            "latestTurnStatus",
                                            "overallStatus",
                                            "summary",
                                            "correctedLatestStudentText"
                                        }
                                }
                        }
                };


            // =====================================================
            // API KEY
            // =====================================================

            var apiKey =
                GetApiKey();


            // =====================================================
            // ANALYSIS MODEL FROM APPSETTINGS
            // =====================================================

            var analysisModelName =
                GetAnalysisModelName();


            // =====================================================
            // GENERATE CONTENT URL
            // =====================================================

            var url =
                "https://generativelanguage.googleapis.com/" +
                "v1beta/models/" +
                $"{Uri.EscapeDataString(analysisModelName)}" +
                ":generateContent";


            // =====================================================
            // HTTP REQUEST
            // =====================================================

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url
                );


            request.Headers.Add(
                "x-goog-api-key",
                apiKey
            );


            request.Content =
                new StringContent(
                    JsonSerializer.Serialize(
                        payload
                    ),
                    Encoding.UTF8,
                    "application/json"
                );


            // =====================================================
            // SEND ANALYSIS REQUEST
            // =====================================================

            using var response =
                await _httpClient.SendAsync(
                    request,
                    cancellationToken
                );


            var responseText =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken
                    );


            // =====================================================
            // ANALYSIS REQUEST FAILED
            // =====================================================

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "Gemini status analysis failed. " +
                    $"HTTP Status: {(int)response.StatusCode}. " +
                    $"Response: {responseText}"
                );
            }


            // =====================================================
            // EXTRACT GEMINI RESPONSE
            // =====================================================

            using var responseDocument =
                JsonDocument.Parse(
                    responseText
                );


            var root =
                responseDocument
                    .RootElement;


            if (!root.TryGetProperty(
                "candidates",
                out var candidates) ||

                candidates.ValueKind !=
                    JsonValueKind.Array ||

                candidates.GetArrayLength() ==
                    0)
            {
                throw new InvalidOperationException(
                    "Gemini did not return a status analysis."
                );
            }


            var firstCandidate =
                candidates[0];


            if (!firstCandidate.TryGetProperty(
                    "content",
                    out var content) ||

                !content.TryGetProperty(
                    "parts",
                    out var parts) ||

                parts.ValueKind !=
                    JsonValueKind.Array ||

                parts.GetArrayLength() ==
                    0)
            {
                throw new InvalidOperationException(
                    "Gemini returned an invalid analysis response."
                );
            }


            // =====================================================
            // GET JSON TEXT
            // =====================================================

            var firstPart =
                parts[0];


            if (!firstPart.TryGetProperty(
                "text",
                out var textElement))
            {
                throw new InvalidOperationException(
                    "Gemini analysis response did not contain text."
                );
            }


            var analysisJson =
                textElement.GetString();


            if (string.IsNullOrWhiteSpace(
                analysisJson))
            {
                throw new InvalidOperationException(
                    "Gemini returned an empty analysis."
                );
            }


            // =====================================================
            // CONVERT JSON → DETAILED RESULT
            // =====================================================

            var detailedAnalysis =
                JsonSerializer.Deserialize
                    <VoiceBotDetailedAnalysisResult>(
                        analysisJson,

                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive =
                                true
                        }
                    );


            if (detailedAnalysis == null)
            {
                throw new InvalidOperationException(
                    "Unable to read Gemini status analysis."
                );
            }


            // =====================================================
            // CLEAN VALUES
            // =====================================================

            detailedAnalysis.LatestTurnStatus =
                detailedAnalysis
                    .LatestTurnStatus?
                    .Trim()
                ?? string.Empty;


            detailedAnalysis.OverallStatus =
                detailedAnalysis
                    .OverallStatus?
                    .Trim()
                ?? string.Empty;


            detailedAnalysis.Summary =
                detailedAnalysis
                    .Summary?
                    .Trim()
                ?? string.Empty;


            detailedAnalysis.CorrectedLatestStudentText =
                detailedAnalysis
                    .CorrectedLatestStudentText?
                    .Trim()
                ?? string.Empty;


            // =====================================================
            // VALIDATE ALLOWED PROJECT STATUSES
            // =====================================================

            if (!IsValidStatus(
                detailedAnalysis
                    .OverallStatus))
            {
                throw new InvalidOperationException(
                    "Gemini returned an unsupported overall status."
                );
            }


            if (
                !string.IsNullOrWhiteSpace(
                    latestStudentTurn
                ) &&

                !IsValidStatus(
                    detailedAnalysis
                        .LatestTurnStatus
                )
            )
            {
                throw new InvalidOperationException(
                    "Gemini returned an unsupported latest-turn status."
                );
            }


            // =====================================================
            // FINAL CURRENT STATUS
            // =====================================================
            //
            // Use the higher-concern classification between:
            //
            // - latest Student turn
            // - overall conversation
            //
            // If no Student turn exists, use overall only.
            //
            // =====================================================

            var finalStatus =
                detailedAnalysis
                    .OverallStatus;


            if (
                !string.IsNullOrWhiteSpace(
                    latestStudentTurn
                ) &&

                GetStatusRank(
                    detailedAnalysis
                        .LatestTurnStatus
                ) >
                GetStatusRank(
                    detailedAnalysis
                        .OverallStatus
                )
            )
            {
                finalStatus =
                    detailedAnalysis
                        .LatestTurnStatus;
            }


            return new VoiceBotAnalysisResult
            {
                Status =
                    finalStatus,

                Summary =
                    detailedAnalysis
                        .Summary,

                CorrectedLatestStudentText =
                    string.IsNullOrWhiteSpace(
                        latestStudentTurn
                    )
                        ? string.Empty
                        : (
                            string.IsNullOrWhiteSpace(
                                detailedAnalysis
                                    .CorrectedLatestStudentText
                            )
                                ? latestStudentTurn
                                : detailedAnalysis
                                    .CorrectedLatestStudentText
                        )
            };
        }


        // =========================================================
        // GET STATUS RANK
        // =========================================================
        //
        // Used only to choose the higher-concern result between
        // latest-turn status and overall conversation status.
        //
        // This is NOT a weighted mental-health score.
        //
        // =========================================================

        private static int GetStatusRank(
            string? status)
        {
            return status switch
            {
                "Normal" =>
                    0,

                "Moderate" =>
                    1,

                "Severe" =>
                    2,

                "Extremely Severe" =>
                    3,

                _ =>
                    -1
            };
        }


        // =========================================================
        // GET GEMINI API KEY
        // =========================================================

        private string GetApiKey()
        {
            var apiKey =
                _configuration[
                    "Gemini:ApiKey"
                ];


            if (string.IsNullOrWhiteSpace(
                apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured. " +
                    "Please configure Gemini:ApiKey in appsettings.json."
                );
            }


            return apiKey.Trim();
        }


        // =========================================================
        // GET LIVE VOICE MODEL
        // =========================================================

        private string GetLiveModelName()
        {
            var modelName =
                _configuration[
                    "Gemini:LiveVoiceModel"
                ];


            if (string.IsNullOrWhiteSpace(
                modelName))
            {
                throw new InvalidOperationException(
                    "Gemini Live Voice model is not configured. " +
                    "Please configure Gemini:LiveVoiceModel " +
                    "in appsettings.json."
                );
            }


            return modelName.Trim();
        }


        // =========================================================
        // GET VOICE ANALYSIS MODEL
        // =========================================================

        private string GetAnalysisModelName()
        {
            var modelName =
                _configuration[
                    "Gemini:VoiceAnalysisModel"
                ];


            if (string.IsNullOrWhiteSpace(
                modelName))
            {
                throw new InvalidOperationException(
                    "Gemini Voice Analysis model is not configured. " +
                    "Please configure Gemini:VoiceAnalysisModel " +
                    "in appsettings.json."
                );
            }


            return modelName.Trim();
        }


        // =========================================================
        // FORMAT UTC TIME FOR GEMINI TOKEN API
        // =========================================================

        private static string FormatUtcTimestamp(
            DateTimeOffset value)
        {
            return value
                .UtcDateTime
                .ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'"
                );
        }


        // =========================================================
        // VALIDATE PROJECT STATUS
        // =========================================================

        private static bool IsValidStatus(
            string? status)
        {
            return status ==
                       "Normal" ||

                   status ==
                       "Moderate" ||

                   status ==
                       "Severe" ||

                   status ==
                       "Extremely Severe";
        }
    }


    // =============================================================
    // INTERNAL DETAILED STATUS ANALYSIS RESULT
    // =============================================================
    //
    // Used only inside GeminiLiveVoiceService.
    //
    // No database table or migration is required.
    //
    // =============================================================

    public class VoiceBotDetailedAnalysisResult
    {
        public string LatestTurnStatus { get; set; }
            = "Normal";


        public string OverallStatus { get; set; }
            = "Normal";


        public string Summary { get; set; }
            = string.Empty;


        public string CorrectedLatestStudentText { get; set; }
            = string.Empty;
    }


    // =============================================================
    // LIVE STATUS ANALYSIS RESULT
    // =============================================================

    public class VoiceBotAnalysisResult
    {
        public string Status { get; set; }
            = "Normal";


        public string Summary { get; set; }
            = string.Empty;


        public string CorrectedLatestStudentText { get; set; }
            = string.Empty;
    }
}