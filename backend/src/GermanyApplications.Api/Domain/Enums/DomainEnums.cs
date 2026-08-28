namespace GermanyApplications.Api.Domain.Enums;

public enum CourseVersionStatus { Draft, InReview, Published, Archived }
public enum RequirementType { AcademicDegree, SubjectArea, MinimumGrade, Credits, Language, WorkExperience, AptitudeTest, Other }
public enum RequirementOperator { Informational, Required, Equals, OneOf, Minimum, Maximum, MinimumDurationMonths }
public enum IntakeTerm { Summer, Winter, Other }
public enum ApplicationRouteType { UniversityPortal, UniAssist, OtherOfficialPortal }
public enum EligibilityOutcome { PotentiallyAligned, PotentialGaps, InsufficientInformation }
public enum EligibilityItemResult { Met, PotentialGap, Unknown, NotEvaluated }
public enum StudentApplicationStatus { Planning, Preparing, ReadyToSubmit, SubmittedByStudent, AdditionalInformationRequested, DecisionReceived, Offer, Rejected, Withdrawn }
public enum ChecklistDocumentStatus { NotStarted, Preparing, Ready, Submitted, NotApplicable }
public enum NotificationType { DeadlineReminder, ApplicationUpdate, System }
public enum NotificationStatus { Pending, Sent, Failed, Read, Cancelled }
public enum ConsentType { TermsOfService, PrivacyNotice, DeadlineNotifications, ProductAnalytics }
public enum SupportTicketStatus { Open, InProgress, WaitingForStudent, Resolved, Closed }
public enum SupportTicketPriority { Low, Normal, High, Urgent }
