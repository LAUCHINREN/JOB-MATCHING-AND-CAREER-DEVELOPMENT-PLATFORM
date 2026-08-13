using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "CareerAdvisor")]
    public class SkillAssessmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SkillAssessmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: SkillAssessments — assessments this advisor authored
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User)!;

            List<SkillAssessment> assessments = await _context.SkillAssessments
                .Where(a => a.CreatedByUserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            List<int> assessmentIds = assessments.Select(a => a.SkillAssessmentId).ToList();

            Dictionary<int, int> questionCounts = await _context.AssessmentQuestions
                .Where(q => assessmentIds.Contains(q.SkillAssessmentId))
                .GroupBy(q => q.SkillAssessmentId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            Dictionary<int, int> attemptCounts = await _context.AssessmentResults
                .Where(r => assessmentIds.Contains(r.SkillAssessmentId))
                .GroupBy(r => r.SkillAssessmentId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            ViewBag.QuestionCounts = questionCounts;
            ViewBag.AttemptCounts = attemptCounts;
            return View(assessments);
        }

        // GET: SkillAssessments/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SkillAssessmentInput());
        }

        // POST: SkillAssessments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SkillAssessmentInput input)
        {
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            string userId = _userManager.GetUserId(User)!;

            SkillAssessment assessment = new SkillAssessment
            {
                Title = input.Title,
                SkillName = input.SkillName,
                Description = input.Description,
                PassingScore = input.PassingScore,
                DurationMinutes = input.DurationMinutes,
                IsActive = input.IsActive,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SkillAssessments.Add(assessment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Assessment created. Now add some questions to it.";
            return RedirectToAction(nameof(Questions), new { id = assessment.SkillAssessmentId });
        }

        // GET: SkillAssessments/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            SkillAssessment? assessment = await GetOwnedAssessmentAsync(id);
            if (assessment == null) return NotFound();

            return View(new SkillAssessmentInput
            {
                SkillAssessmentId = assessment.SkillAssessmentId,
                Title = assessment.Title,
                SkillName = assessment.SkillName,
                Description = assessment.Description,
                PassingScore = assessment.PassingScore,
                DurationMinutes = assessment.DurationMinutes,
                IsActive = assessment.IsActive
            });
        }

        // POST: SkillAssessments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SkillAssessmentInput input)
        {
            SkillAssessment? assessment = await GetOwnedAssessmentAsync(id);
            if (assessment == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            assessment.Title = input.Title;
            assessment.SkillName = input.SkillName;
            assessment.Description = input.Description;
            assessment.PassingScore = input.PassingScore;
            assessment.DurationMinutes = input.DurationMinutes;
            assessment.IsActive = input.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Assessment updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: SkillAssessments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            SkillAssessment? assessment = await GetOwnedAssessmentAsync(id);
            if (assessment == null) return NotFound();

            bool hasResults = await _context.AssessmentResults.AnyAsync(r => r.SkillAssessmentId == id);
            if (hasResults)
            {
                TempData["ErrorMessage"] = "This assessment cannot be deleted because job seekers have already taken it.";
                return RedirectToAction(nameof(Index));
            }

            List<AssessmentQuestion> questions = await _context.AssessmentQuestions
                .Where(q => q.SkillAssessmentId == id)
                .ToListAsync();
            _context.AssessmentQuestions.RemoveRange(questions);

            _context.SkillAssessments.Remove(assessment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Assessment deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: SkillAssessments/Questions/5 — manage questions for one assessment
        [HttpGet]
        public async Task<IActionResult> Questions(int id)
        {
            SkillAssessment? assessment = await GetOwnedAssessmentAsync(id);
            if (assessment == null) return NotFound();

            List<AssessmentQuestion> questions = await _context.AssessmentQuestions
                .Where(q => q.SkillAssessmentId == id)
                .OrderBy(q => q.AssessmentQuestionId)
                .ToListAsync();

            ViewBag.Assessment = assessment;
            return View(questions);
        }

        // GET: SkillAssessments/CreateQuestion/5
        [HttpGet]
        public async Task<IActionResult> CreateQuestion(int id)
        {
            SkillAssessment? assessment = await GetOwnedAssessmentAsync(id);
            if (assessment == null) return NotFound();

            ViewBag.Assessment = assessment;
            return View(new AssessmentQuestionInput { SkillAssessmentId = id });
        }

        // POST: SkillAssessments/CreateQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion(AssessmentQuestionInput input)
        {
            SkillAssessment? assessment = await GetOwnedAssessmentAsync(input.SkillAssessmentId);
            if (assessment == null) return NotFound();

            NormalizeCorrectAnswer(input);

            if (!ModelState.IsValid)
            {
                ViewBag.Assessment = assessment;
                return View(input);
            }

            AssessmentQuestion question = new AssessmentQuestion
            {
                SkillAssessmentId = input.SkillAssessmentId,
                QuestionText = input.QuestionText,
                OptionA = input.OptionA,
                OptionB = input.OptionB,
                OptionC = input.OptionC,
                OptionD = input.OptionD,
                CorrectAnswer = input.CorrectAnswer
            };

            _context.AssessmentQuestions.Add(question);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question added.";
            return RedirectToAction(nameof(Questions), new { id = input.SkillAssessmentId });
        }

        // GET: SkillAssessments/EditQuestion/5
        [HttpGet]
        public async Task<IActionResult> EditQuestion(int id)
        {
            AssessmentQuestion? question = await _context.AssessmentQuestions.FirstOrDefaultAsync(q => q.AssessmentQuestionId == id);
            if (question == null) return NotFound();

            SkillAssessment? assessment = await GetOwnedAssessmentAsync(question.SkillAssessmentId);
            if (assessment == null) return NotFound();

            ViewBag.Assessment = assessment;
            return View(new AssessmentQuestionInput
            {
                AssessmentQuestionId = question.AssessmentQuestionId,
                SkillAssessmentId = question.SkillAssessmentId,
                QuestionText = question.QuestionText,
                OptionA = question.OptionA,
                OptionB = question.OptionB,
                OptionC = question.OptionC,
                OptionD = question.OptionD,
                CorrectAnswer = question.CorrectAnswer
            });
        }

        // POST: SkillAssessments/EditQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, AssessmentQuestionInput input)
        {
            AssessmentQuestion? question = await _context.AssessmentQuestions.FirstOrDefaultAsync(q => q.AssessmentQuestionId == id);
            if (question == null) return NotFound();

            SkillAssessment? assessment = await GetOwnedAssessmentAsync(question.SkillAssessmentId);
            if (assessment == null) return NotFound();

            NormalizeCorrectAnswer(input);

            if (!ModelState.IsValid)
            {
                ViewBag.Assessment = assessment;
                return View(input);
            }

            question.QuestionText = input.QuestionText;
            question.OptionA = input.OptionA;
            question.OptionB = input.OptionB;
            question.OptionC = input.OptionC;
            question.OptionD = input.OptionD;
            question.CorrectAnswer = input.CorrectAnswer;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question updated.";
            return RedirectToAction(nameof(Questions), new { id = question.SkillAssessmentId });
        }

        // POST: SkillAssessments/DeleteQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            AssessmentQuestion? question = await _context.AssessmentQuestions.FirstOrDefaultAsync(q => q.AssessmentQuestionId == id);
            if (question == null) return NotFound();

            SkillAssessment? assessment = await GetOwnedAssessmentAsync(question.SkillAssessmentId);
            if (assessment == null) return NotFound();

            _context.AssessmentQuestions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question removed.";
            return RedirectToAction(nameof(Questions), new { id = question.SkillAssessmentId });
        }

        private async Task<SkillAssessment?> GetOwnedAssessmentAsync(int id)
        {
            string userId = _userManager.GetUserId(User)!;

            return await _context.SkillAssessments
                .FirstOrDefaultAsync(a => a.SkillAssessmentId == id && a.CreatedByUserId == userId);
        }

        private static void NormalizeCorrectAnswer(AssessmentQuestionInput input)
        {
            input.CorrectAnswer = input.CorrectAnswer?.Trim().ToUpperInvariant() ?? string.Empty;
        }
    }
}
