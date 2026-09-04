/**
 * Universal Compact Direct-Download PDF Engine
 * Student Mental Health Monitoring System (SMHMS)
 * 
 * Guarantees:
 * 1. EXACT SAME on-screen Chart.js line graph captured at full-width (100% width) with zero distortion
 * 2. Dense, compact, zero-wasted-space clinical layout
 * 3. Seamless natural content flow with clean page-break-inside avoidance (no artificial empty gaps)
 * 4. Automatic high-resolution rendering with official institutional header and verification footer
 */

(function () {
    // Inject ultra-compact, zero-empty-space PDF styling
    function injectPdfStyles() {
        if (document.getElementById('smhms-pdf-compact-styles')) return;
        const style = document.createElement('style');
        style.id = 'smhms-pdf-compact-styles';
        style.innerHTML = `
            /* Hide non-print interactive controls */
            .pdf-exporting-mode .btn,
            .pdf-exporting-mode button,
            .pdf-exporting-mode .no-print,
            .pdf-exporting-mode .admin-sidebar,
            .pdf-exporting-mode .department-sidebar,
            .pdf-exporting-mode .student-sidebar,
            .pdf-exporting-mode nav,
            .pdf-exporting-mode .navbar,
            .pdf-exporting-mode .pagination,
            .pdf-exporting-mode input,
            .pdf-exporting-mode select,
            .pdf-exporting-mode .btn-close,
            .pdf-exporting-mode .dropdown-menu {
                display: none !important;
            }

            /* Master Container Settings - Full Width & Zero Wasted Margins */
            .pdf-exporting-mode {
                background: #FFFFFF !important;
                color: #0F172A !important;
                padding: 0 !important;
                margin: 0 auto !important;
                width: 100% !important;
                max-width: 100% !important;
                font-size: 9px !important;
                line-height: 1.25 !important;
                -webkit-print-color-adjust: exact !important;
                print-color-adjust: exact !important;
                box-sizing: border-box !important;
            }

            /* Ultra-Compact Cards */
            .pdf-exporting-mode .card,
            .pdf-exporting-mode .glass-card,
            .pdf-exporting-mode .metric-card-admin,
            .pdf-exporting-mode .glass-card-admin,
            .pdf-exporting-mode .glass-card-dept {
                box-shadow: none !important;
                margin-bottom: 5px !important;
                border: 1px solid #CBD5E1 !important;
                border-radius: 5px !important;
                page-break-inside: avoid !important;
                break-inside: avoid !important;
                background-clip: padding-box !important;
            }

            .pdf-exporting-mode .card-body {
                padding: 5px 8px !important;
                min-height: auto !important;
            }

            .pdf-exporting-mode .card-header {
                padding: 4px 8px !important;
            }

            /* Ensure all height restrictions collapse to natural compact size */
            .pdf-exporting-mode [style*="min-height"],
            .pdf-exporting-mode [style*="height: 3"],
            .pdf-exporting-mode [style*="height: 2"] {
                min-height: auto !important;
            }

            /* Headings & Text */
            .pdf-exporting-mode h1,
            .pdf-exporting-mode h2 {
                font-size: 14px !important;
                margin: 0 0 2px 0 !important;
                line-height: 1.2 !important;
            }

            .pdf-exporting-mode h3,
            .pdf-exporting-mode h4 {
                font-size: 12px !important;
                margin: 0 0 2px 0 !important;
                line-height: 1.2 !important;
            }

            .pdf-exporting-mode h5,
            .pdf-exporting-mode h6 {
                font-size: 10.5px !important;
                margin: 0 0 2px 0 !important;
                line-height: 1.2 !important;
            }

            .pdf-exporting-mode p,
            .pdf-exporting-mode span,
            .pdf-exporting-mode small,
            .pdf-exporting-mode label,
            .pdf-exporting-mode li {
                font-size: 8.5px !important;
                line-height: 1.25 !important;
                margin-bottom: 0 !important;
            }

            /* Badges */
            .pdf-exporting-mode .badge {
                font-size: 8px !important;
                padding: 2px 5px !important;
                font-weight: 700 !important;
                border-radius: 4px !important;
                display: inline-block !important;
                -webkit-print-color-adjust: exact !important;
                print-color-adjust: exact !important;
            }

            /* Full-Width Chart Image Replacement */
            .pdf-exporting-mode .smhms-pdf-chart-img {
                width: 100% !important;
                height: auto !important;
                max-height: 240px !important;
                display: block !important;
                margin: 0 auto !important;
                object-fit: fill !important;
                background: #FFFFFF !important;
                page-break-inside: avoid !important;
                break-inside: avoid !important;
            }

            /* Tables with Tight Clinical Spacing */
            .pdf-exporting-mode table {
                font-size: 8px !important;
                margin-bottom: 3px !important;
                width: 100% !important;
                border-collapse: collapse !important;
            }

            .pdf-exporting-mode thead {
                display: table-header-group !important;
            }

            .pdf-exporting-mode tr {
                page-break-inside: avoid !important;
                break-inside: avoid !important;
            }

            .pdf-exporting-mode th,
            .pdf-exporting-mode td {
                padding: 3px 5px !important;
                line-height: 1.2 !important;
                vertical-align: middle !important;
                border-color: #E2E8F0 !important;
            }

            /* Compact Bootstrap Grid - Zero Waste */
            .pdf-exporting-mode .row {
                margin-left: -3px !important;
                margin-right: -3px !important;
                margin-bottom: 3px !important;
            }

            .pdf-exporting-mode [class*="col-"] {
                padding-left: 3px !important;
                padding-right: 3px !important;
            }

            .pdf-exporting-mode .mb-4,
            .pdf-exporting-mode .my-4 {
                margin-bottom: 5px !important;
            }

            .pdf-exporting-mode .mb-3,
            .pdf-exporting-mode .my-3 {
                margin-bottom: 4px !important;
            }

            .pdf-exporting-mode .p-4 {
                padding: 5px 8px !important;
            }

            .pdf-exporting-mode .p-3 {
                padding: 4px 6px !important;
            }

            .pdf-exporting-mode .g-3,
            .pdf-exporting-mode .g-4 {
                --bs-gutter-x: 6px !important;
                --bs-gutter-y: 6px !important;
            }
        `;
        document.head.appendChild(style);
    }

    injectPdfStyles();

    /**
     * Download Compact PDF Report Directly to Disk
     * @param {string|HTMLElement} elementOrSelector - Container ID or DOM element
     * @param {object} options - Configuration options
     */
    window.downloadReportPdf = function (elementOrSelector, options) {
        options = options || {};
        const title = options.title || document.title || 'SMHMS_Report';
        const safeTitle = (options.filename || title).replace(/[^a-zA-Z0-9_-]/g, '_').replace(/_+/g, '_');
        const filename = safeTitle + '.pdf';
        const orientation = options.orientation || 'portrait';

        let targetEl = null;
        if (typeof elementOrSelector === 'string') {
            targetEl = document.querySelector(elementOrSelector) || document.getElementById(elementOrSelector.replace('#', ''));
        } else if (elementOrSelector instanceof HTMLElement) {
            targetEl = elementOrSelector;
        }

        if (!targetEl) {
            console.error('Target element for PDF download not found:', elementOrSelector);
            return;
        }

        // Show downloading status badge
        let loadingBadge = document.getElementById('smhms-pdf-loading');
        if (!loadingBadge) {
            loadingBadge = document.createElement('div');
            loadingBadge.id = 'smhms-pdf-loading';
            loadingBadge.style.cssText = 'position:fixed;bottom:24px;right:24px;background:#0F172A;color:#FFFFFF;padding:10px 20px;border-radius:8px;box-shadow:0 10px 25px rgba(0,0,0,0.3);z-index:9999999;font-family:system-ui,sans-serif;font-size:12.5px;font-weight:600;display:flex;align-items:center;gap:10px;';
            loadingBadge.innerHTML = '<span class="spinner-border spinner-border-sm text-warning" role="status" style="width:1rem;height:1rem;border-width:2px;"></span> Generating Compact PDF...';
            document.body.appendChild(loadingBadge);
        }
        loadingBadge.style.display = 'flex';

        // Add compact institutional header
        const headerDiv = document.createElement('div');
        headerDiv.id = 'smhms-temp-pdf-header';
        headerDiv.style.cssText = 'border-bottom: 2px solid #004D25; padding-bottom: 5px; margin-bottom: 6px; display: flex; justify-content: space-between; align-items: flex-end; font-family: system-ui, sans-serif;';
        headerDiv.innerHTML = `
            <div>
                <h2 style="margin: 0; color: #004D25; font-size: 13.5px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.5px;">
                    STUDENT MENTAL HEALTH MONITORING SYSTEM
                </h2>
                <p style="margin: 1px 0 0 0; color: #475569; font-size: 8.5px; font-weight: 600;">
                    Official Health & Clinical Psychological Records • Confidential Case Report
                </p>
            </div>
            <div style="text-align: right;">
                <span style="display: inline-block; background: #004D25; color: #FFFFFF; font-size: 7.5px; font-weight: 800; padding: 2px 6px; border-radius: 3px; text-transform: uppercase;">
                    OFFICIAL REPORT
                </span>
                <p style="margin: 1px 0 0 0; color: #64748B; font-size: 8px; font-weight: 500;">
                    Date: ${new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
                </p>
            </div>
        `;
        targetEl.insertBefore(headerDiv, targetEl.firstChild);

        // Add compact official verification footer
        const footerDiv = document.createElement('div');
        footerDiv.id = 'smhms-temp-pdf-footer';
        footerDiv.style.cssText = 'margin-top: 8px; padding-top: 5px; border-top: 1px solid #CBD5E1; font-family: system-ui, sans-serif; font-size: 8px; color: #64748B; display: flex; justify-content: space-between; align-items: flex-end; page-break-inside: avoid; break-inside: avoid;';
        footerDiv.innerHTML = `
            <div>
                <p style="margin: 0; font-weight: 700; color: #334155; font-size: 8px;">CONFIDENTIALITY NOTICE:</p>
                <p style="margin: 1px 0 0 0; font-size: 7px; line-height: 1.25; max-width: 460px;">
                    This document contains confidential mental health screening evaluation records. Unauthorized distribution is strictly prohibited.
                </p>
            </div>
            <div style="text-align: right; min-width: 130px;">
                <div style="border-bottom: 1px dashed #94A3B8; width: 90px; margin-left: auto; height: 16px;"></div>
                <p style="margin: 2px 0 0 0; font-size: 7.5px; font-weight: 700; color: #1E293B;">Authorized Officer Sign</p>
                <p style="margin: 0; font-size: 6.5px; color: #64748B;">System Verified Record • SMHMS</p>
            </div>
        `;
        targetEl.appendChild(footerDiv);

        // Capture EXACT on-screen Chart.js canvases at 100% FULL WIDTH
        const canvasReplacements = [];
        const canvases = targetEl.querySelectorAll('canvas');
        canvases.forEach(function (cvs) {
            try {
                // Get exact dimensions from on-screen canvas
                const w = cvs.width || (cvs.offsetWidth * 2) || 1200;
                const h = cvs.height || (cvs.offsetHeight * 2) || 450;

                // Create offscreen canvas with solid white background to prevent transparency artifacts
                const tempCanvas = document.createElement('canvas');
                tempCanvas.width = w;
                tempCanvas.height = h;
                const tempCtx = tempCanvas.getContext('2d');

                tempCtx.fillStyle = '#FFFFFF';
                tempCtx.fillRect(0, 0, w, h);

                // Draw the EXACT on-screen rendered Chart.js line graph
                tempCtx.drawImage(cvs, 0, 0, w, h);

                const img = document.createElement('img');
                img.src = tempCanvas.toDataURL('image/png', 1.0);
                img.className = 'smhms-pdf-chart-img';
                img.style.cssText = 'width: 100% !important; height: auto !important; max-height: 240px !important; display: block !important; margin: 0 auto !important; object-fit: fill !important; background: #FFFFFF !important;';

                // Ensure parent container takes full width with zero artificial height
                const parent = cvs.parentNode;
                if (parent) {
                    parent.style.height = 'auto';
                    parent.style.minHeight = 'auto';
                    parent.style.width = '100%';
                    parent.style.padding = '2px 0';
                }

                // Hide original canvas and insert rendered high-res full-width image
                cvs.style.display = 'none';
                cvs.parentNode.insertBefore(img, cvs);
                canvasReplacements.push({ cvs: cvs, img: img, parent: parent });
            } catch (e) {
                console.warn('Canvas conversion skipped:', e);
            }
        });

        // Check for broken images and replace them with fallback avatars
        const brokenImgReplacements = [];
        const images = targetEl.querySelectorAll('img');
        images.forEach(function (img) {
            if (img.classList.contains('smhms-pdf-chart-img')) return;
            if (!img.complete || img.naturalWidth === 0) {
                const origSrc = img.src;
                img.src = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="36" height="36" viewBox="0 0 36 36"><circle cx="18" cy="18" r="18" fill="%23E2E8F0"/><circle cx="18" cy="13" r="6" fill="%2394A3B8"/><path d="M5 30 A13 13 0 0 1 31 30 Z" fill="%2394A3B8"/></svg>';
                brokenImgReplacements.push({ img: img, origSrc: origSrc });
            }
        });

        // Apply compact exporting styling
        targetEl.classList.add('pdf-exporting-mode');

        let isCleanedUp = false;
        function cleanup() {
            if (isCleanedUp) return;
            isCleanedUp = true;

            targetEl.classList.remove('pdf-exporting-mode');
            if (targetEl.contains(headerDiv)) {
                targetEl.removeChild(headerDiv);
            }
            if (targetEl.contains(footerDiv)) {
                targetEl.removeChild(footerDiv);
            }
            // Restore canvases
            canvasReplacements.forEach(function (item) {
                if (item.img.parentNode) {
                    item.img.parentNode.removeChild(item.img);
                }
                item.cvs.style.display = '';
                if (item.parent) {
                    item.parent.style.height = '';
                    item.parent.style.minHeight = '';
                    item.parent.style.width = '';
                    item.parent.style.padding = '';
                }
            });
            // Restore broken images
            brokenImgReplacements.forEach(function (item) {
                item.img.src = item.origSrc;
            });
            if (loadingBadge) {
                loadingBadge.style.display = 'none';
            }
        }

        // Ultra-compact html2pdf configuration - Zero empty space
        const opt = {
            margin: [0, 0, 0, 0],
            filename: filename,
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: {
                scale: 2,
                useCORS: true,
                allowTaint: true,
                logging: false,
                imageTimeout: 5000,
                backgroundColor: '#FFFFFF',
                scrollY: 0,
                scrollX: 0
            },
            jsPDF: {
                unit: 'mm',
                format: 'a4',
                orientation: orientation
            },
            pagebreak: { mode: ['avoid-all', 'css', 'legacy'] }
        };

        // Safety timeout to prevent hanging
        const safetyTimer = setTimeout(cleanup, 15000);

        function startExport() {
            if (typeof html2pdf !== 'undefined') {
                html2pdf().set(opt).from(targetEl).save().then(function () {
                    clearTimeout(safetyTimer);
                    cleanup();
                }).catch(function (err) {
                    clearTimeout(safetyTimer);
                    console.error('html2pdf direct download error:', err);
                    cleanup();
                });
            } else {
                clearTimeout(safetyTimer);
                cleanup();
            }
        }

        setTimeout(startExport, 150);
    };
})();
