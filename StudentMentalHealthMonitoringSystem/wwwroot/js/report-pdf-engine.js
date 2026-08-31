/**
 * Universal Direct-Download PDF Engine
 * Student Mental Health Monitoring System (SMHMS)
 */

(function () {
    // Inject clean PDF styling rules (strictly standard CSS properties without CSS variables)
    function injectPdfStyles() {
        if (document.getElementById('smhms-pdf-engine-styles')) return;
        const style = document.createElement('style');
        style.id = 'smhms-pdf-engine-styles';
        style.innerHTML = `
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
            .pdf-exporting-mode select {
                display: none !important;
            }

            .pdf-exporting-mode {
                background: #ffffff !important;
                color: #0F172A !important;
                padding: 4px 6px !important;
                margin: 0 !important;
                font-size: 10px !important;
                line-height: 1.3 !important;
            }

            .pdf-exporting-mode .card,
            .pdf-exporting-mode .metric-card-admin,
            .pdf-exporting-mode .glass-card-admin,
            .pdf-exporting-mode .glass-card-dept,
            .pdf-exporting-mode .glass-card {
                border: 1px solid #CBD5E1 !important;
                box-shadow: none !important;
                margin-bottom: 6px !important;
                border-radius: 6px !important;
                background: #ffffff !important;
                page-break-inside: avoid !important;
            }

            .pdf-exporting-mode .card-body {
                padding: 6px 10px !important;
            }

            .pdf-exporting-mode h1,
            .pdf-exporting-mode h2 {
                font-size: 14px !important;
                margin-bottom: 3px !important;
                line-height: 1.2 !important;
            }

            .pdf-exporting-mode h3,
            .pdf-exporting-mode h4 {
                font-size: 12px !important;
                margin-bottom: 2px !important;
                line-height: 1.2 !important;
            }

            .pdf-exporting-mode h5,
            .pdf-exporting-mode h6 {
                font-size: 11px !important;
                margin-bottom: 2px !important;
            }

            .pdf-exporting-mode p,
            .pdf-exporting-mode span,
            .pdf-exporting-mode small,
            .pdf-exporting-mode label {
                font-size: 9.5px !important;
                line-height: 1.25 !important;
            }

            .pdf-exporting-mode .badge {
                font-size: 8px !important;
                padding: 2px 5px !important;
                font-weight: bold !important;
            }

            .pdf-exporting-mode img.rounded-circle,
            .pdf-exporting-mode img {
                max-width: 40px !important;
                max-height: 40px !important;
            }

            .pdf-exporting-mode table {
                font-size: 9px !important;
                margin-bottom: 4px !important;
                width: 100% !important;
                border-collapse: collapse !important;
            }

            .pdf-exporting-mode tr {
                page-break-inside: avoid !important;
            }

            .pdf-exporting-mode th,
            .pdf-exporting-mode td {
                padding: 3px 5px !important;
                line-height: 1.2 !important;
            }

            .pdf-exporting-mode svg,
            .pdf-exporting-mode canvas {
                max-height: 180px !important;
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
            loadingBadge.style.cssText = 'position:fixed;bottom:24px;right:24px;background:#0F172A;color:#fff;padding:12px 22px;border-radius:10px;box-shadow:0 10px 30px rgba(0,0,0,0.35);z-index:9999999;font-family:system-ui,sans-serif;font-size:13px;font-weight:600;display:flex;align-items:center;gap:10px;';
            loadingBadge.innerHTML = '<span class="spinner-border spinner-border-sm text-warning" role="status" style="width:1rem;height:1rem;border-width:2px;"></span> Generating PDF...';
            document.body.appendChild(loadingBadge);
        }
        loadingBadge.style.display = 'flex';

        // Add compact official header
        const headerDiv = document.createElement('div');
        headerDiv.id = 'smhms-temp-pdf-header';
        headerDiv.style.cssText = 'border-bottom: 2.5px solid #004D25; padding-bottom: 8px; margin-bottom: 10px; display: flex; justify-content: space-between; align-items: flex-end; font-family: system-ui, sans-serif;';
        headerDiv.innerHTML = `
            <div>
                <h2 style="margin: 0; color: #004D25; font-size: 15px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.5px;">
                    Student Mental Health Monitoring System
                </h2>
                <p style="margin: 2px 0 0 0; color: #475569; font-size: 10px; font-weight: 600;">
                    Official Health & Clinical Psychological Records • Confidential Document
                </p>
            </div>
            <div style="text-align: right;">
                <span style="display: inline-block; background: #004D25; color: #ffffff; font-size: 8px; font-weight: 800; padding: 2px 6px; border-radius: 3px; text-transform: uppercase;">
                    OFFICIAL REPORT
                </span>
                <p style="margin: 2px 0 0 0; color: #64748B; font-size: 9px; font-weight: 500;">
                    Date: ${new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
                </p>
            </div>
        `;
        targetEl.insertBefore(headerDiv, targetEl.firstChild);

        // Add compact official footer
        const footerDiv = document.createElement('div');
        footerDiv.id = 'smhms-temp-pdf-footer';
        footerDiv.style.cssText = 'margin-top: 14px; padding-top: 8px; border-top: 1px solid #CBD5E1; font-family: system-ui, sans-serif; font-size: 8.5px; color: #64748B; display: flex; justify-content: space-between; align-items: flex-end; page-break-inside: avoid;';
        footerDiv.innerHTML = `
            <div>
                <p style="margin: 0; font-weight: 700; color: #334155; font-size: 8.5px;">CONFIDENTIALITY NOTICE:</p>
                <p style="margin: 2px 0 0 0; font-size: 7.5px; line-height: 1.25; max-width: 480px;">
                    This document contains confidential health and academic screening evaluation records. Unauthorized distribution or copying is strictly prohibited.
                </p>
            </div>
            <div style="text-align: right; min-width: 140px;">
                <div style="border-bottom: 1px dashed #94A3B8; width: 100px; margin-left: auto; height: 18px;"></div>
                <p style="margin: 2px 0 0 0; font-size: 8px; font-weight: 700; color: #1E293B;">Authorized Officer Sign</p>
                <p style="margin: 0; font-size: 7px; color: #64748B;">System Verified Record</p>
            </div>
        `;
        targetEl.appendChild(footerDiv);

        // Swap Chart.js canvas elements with images to avoid html2canvas empty canvas bug
        const canvasReplacements = [];
        const canvases = targetEl.querySelectorAll('canvas');
        canvases.forEach(function (cvs) {
            try {
                const img = document.createElement('img');
                img.src = cvs.toDataURL('image/png');
                img.style.width = (cvs.offsetWidth || cvs.width || 300) + 'px';
                img.style.height = (cvs.offsetHeight || cvs.height || 180) + 'px';
                img.style.maxWidth = '100%';
                img.className = 'smhms-temp-chart-img';
                
                cvs.style.display = 'none';
                cvs.parentNode.insertBefore(img, cvs);
                canvasReplacements.push({ cvs: cvs, img: img });
            } catch (e) {
                console.warn('Canvas conversion skipped:', e);
            }
        });

        // Check for broken images and replace them with fallback avatars
        const brokenImgReplacements = [];
        const images = targetEl.querySelectorAll('img');
        images.forEach(function (img) {
            if (img.classList.contains('smhms-temp-chart-img')) return;
            if (!img.complete || img.naturalWidth === 0) {
                const origSrc = img.src;
                img.src = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 40 40"><circle cx="20" cy="20" r="20" fill="%23E2E8F0"/><circle cx="20" cy="15" r="7" fill="%2394A3B8"/><path d="M6 34 A14 14 0 0 1 34 34 Z" fill="%2394A3B8"/></svg>';
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
            });
            // Restore broken images
            brokenImgReplacements.forEach(function (item) {
                item.img.src = item.origSrc;
            });
            if (loadingBadge) {
                loadingBadge.style.display = 'none';
            }
        }

        // Compact html2pdf options
        const opt = {
            margin: [5, 6, 5, 6],
            filename: filename,
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: {
                scale: 2,
                useCORS: true,
                allowTaint: true,
                logging: false,
                imageTimeout: 3000,
                backgroundColor: '#ffffff'
            },
            jsPDF: {
                unit: 'mm',
                format: 'a4',
                orientation: orientation
            },
            pagebreak: { mode: ['avoid-all', 'css', 'legacy'] }
        };

        // Safety timeout to prevent hanging forever
        const safetyTimer = setTimeout(function () {
            cleanup();
        }, 12000);

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

        setTimeout(startExport, 120);
    };
})();
